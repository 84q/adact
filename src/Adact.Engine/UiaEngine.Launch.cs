using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Adact.Engine.Exceptions;

using Microsoft.Extensions.Logging;

namespace Adact.Engine;

public sealed partial class UiaEngine
{
    /// <summary>UWP モードを示す入力プレフィックス (case-insensitive)。設計 024 §2。</summary>
    private const string UwpPrefix = "shell:AppsFolder\\";

    /// <summary>
    /// プロセスを起動する。<see cref="LaunchRequest.Executable"/> が <c>shell:AppsFolder\</c> で始まる場合は
    /// UWP / Packaged アプリとして <see cref="NativeMethods.IApplicationActivationManager.ActivateApplication"/>
    /// 経由で起動し、それ以外は <see cref="Process.Start(ProcessStartInfo)"/> 経由で起動する。
    /// 設計 024 §2 / §3。
    /// </summary>
    /// <param name="request">起動要求。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>起動結果 (PID / プロセス名 / 解決済みパス)。</returns>
    /// <exception cref="ObjectDisposedException">本 Engine が Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> が null。</exception>
    /// <exception cref="ArgumentException">UWP モードで cwd または env が指定された場合 (設計 024 §3)。</exception>
    /// <exception cref="LaunchFailedException">起動に失敗した場合。</exception>
    public Task<LaunchResult> LaunchAsync(LaunchRequest request, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Executable))
        {
            throw new ArgumentException("executable must not be empty.", nameof(request));
        }

        var isUwp = request.Executable.StartsWith(UwpPrefix, StringComparison.OrdinalIgnoreCase);

        if (isUwp)
        {
            if (!string.IsNullOrEmpty(request.WorkingDirectory))
            {
                throw new ArgumentException(
                    "workingDirectory is unsupported with UWP launch.", nameof(request));
            }
            if (request.Environment is { Count: > 0 })
            {
                throw new ArgumentException(
                    "environment is unsupported with UWP launch.", nameof(request));
            }

            var aumid = request.Executable.Substring(UwpPrefix.Length);
            return Task.FromResult(LaunchUwp(aumid, request.Arguments));
        }

        return Task.FromResult(LaunchWin32(request));
    }

    /// <summary>Win32 / .NET 実行ファイルを <see cref="Process.Start(ProcessStartInfo)"/> で起動する。</summary>
    /// <param name="request">起動要求。</param>
    /// <returns>起動結果。</returns>
    private LaunchResult LaunchWin32(LaunchRequest request)
    {
        var psi = new ProcessStartInfo
        {
            FileName = request.Executable,
            UseShellExecute = false,
        };

        if (!string.IsNullOrEmpty(request.WorkingDirectory))
        {
            psi.WorkingDirectory = request.WorkingDirectory;
        }

        if (request.Arguments is { Count: > 0 })
        {
            foreach (var a in request.Arguments)
            {
                psi.ArgumentList.Add(a ?? string.Empty);
            }
        }

        if (request.Environment is { Count: > 0 })
        {
            foreach (var kv in request.Environment)
            {
                psi.Environment[kv.Key] = kv.Value;
            }
        }

        Process? process;
        try
        {
            process = Process.Start(psi);
        }
        catch (Win32Exception ex)
        {
            throw new LaunchFailedException(
                $"failed to launch '{request.Executable}': {ex.Message}", ex);
        }
        catch (System.IO.FileNotFoundException ex)
        {
            throw new LaunchFailedException(
                $"failed to launch '{request.Executable}': {ex.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new LaunchFailedException(
                $"failed to launch '{request.Executable}': {ex.Message}", ex);
        }

        if (process is null)
        {
            throw new LaunchFailedException(
                $"failed to launch '{request.Executable}': Process.Start returned null.");
        }

        var pid = process.Id;
        string processName;
        string? executablePath = null;
        try
        {
            processName = process.ProcessName;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read ProcessName for pid {Pid}; using basename fallback", pid);
            processName = System.IO.Path.GetFileNameWithoutExtension(request.Executable);
        }

        try
        {
            executablePath = process.MainModule?.FileName;
        }
        catch (Exception ex)
        {
            // 権限不足 (例: x86/x64 不一致 / 別ユーザ昇格) で取れないことがある。要件 §5 で null 許容。
            _logger.LogDebug(ex, "Failed to read MainModule.FileName for pid {Pid}; returning null", pid);
        }

        return new LaunchResult(pid, processName, executablePath);
    }

    /// <summary>UWP / Packaged アプリを ApplicationActivationManager 経由で起動する。</summary>
    /// <param name="aumid">起動対象の AUMID。</param>
    /// <param name="arguments">引数 (UWP では単一の文字列に連結する)。</param>
    /// <returns>起動結果。</returns>
    private LaunchResult LaunchUwp(string aumid, IReadOnlyList<string>? arguments)
    {
        if (string.IsNullOrWhiteSpace(aumid))
        {
            throw new LaunchFailedException("UWP launch requires a non-empty AUMID after 'shell:AppsFolder\\'.");
        }

        // UWP はネイティブ側で 1 本の引数文字列を受け取るため、
        // ProcessStartInfo.ArgumentList と異なり手動で連結する。空白を含む引数は "..." で囲む。
        var argString = arguments is { Count: > 0 }
            ? string.Join(' ', arguments.Select(QuoteIfNeeded))
            : string.Empty;

        object? comObject = null;
        try
        {
            var clsid = NativeMethods.CLSID_ApplicationActivationManager;
            var type = Type.GetTypeFromCLSID(clsid)
                ?? throw new LaunchFailedException(
                    "ApplicationActivationManager COM class is not registered on this system.");
            comObject = Activator.CreateInstance(type)
                ?? throw new LaunchFailedException(
                    "Failed to create ApplicationActivationManager COM instance.");
            var manager = (NativeMethods.IApplicationActivationManager)comObject;

            var hr = manager.ActivateApplication(aumid, argString, NativeMethods.AO_NOERRORUI, out var pid);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            if (pid == 0)
            {
                throw new LaunchFailedException(
                    $"UWP launch returned PID 0 for '{aumid}'.");
            }

            string processName = aumid;
            try
            {
                processName = Process.GetProcessById((int)pid).ProcessName;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read ProcessName for UWP pid {Pid}; using AUMID fallback", pid);
            }

            return new LaunchResult((int)pid, processName, aumid);
        }
        catch (LaunchFailedException)
        {
            throw;
        }
        catch (COMException ex)
        {
            throw new LaunchFailedException(
                $"UWP launch failed for '{aumid}' (HRESULT 0x{ex.HResult:X8}): {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new LaunchFailedException(
                $"UWP launch failed for '{aumid}': {ex.Message}", ex);
        }
        finally
        {
            if (comObject is not null)
            {
                try { Marshal.FinalReleaseComObject(comObject); } catch { }
            }
        }
    }

    /// <summary>
    /// 単一引数を Win32 <c>CommandLineToArgvW</c> 規約に従ってクォーティングする
    /// (UWP <see cref="NativeMethods.IApplicationActivationManager.ActivateApplication"/> が
    /// 単一引数文字列を要求するため)。
    /// .NET runtime の <c>System.PasteArguments.AppendArgument</c> を移植したロジック:
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/PasteArguments.cs
    /// </summary>
    /// <remarks>
    /// 空白 / タブ / <c>"</c> を含まない引数は素のまま返す。それ以外は <c>"..."</c> で囲み、
    /// 末尾バックスラッシュ列および <c>"</c> 直前のバックスラッシュ列は個数を 2 倍 (+1)
    /// にして閉じクオートが誤エスケープされないようにする。
    /// </remarks>
    /// <param name="arg">引数。</param>
    /// <returns>必要に応じてクォートされた引数。</returns>
    internal static string QuoteIfNeeded(string arg)
    {
        if (arg is null) return "\"\"";
        if (arg.Length != 0 && arg.IndexOfAny([' ', '\t', '"']) < 0)
        {
            // クォート不要なケース。バックスラッシュは触らない。
            return arg;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append('"');
        var idx = 0;
        while (idx < arg.Length)
        {
            var c = arg[idx++];
            if (c == '\\')
            {
                var numBackslash = 1;
                while (idx < arg.Length && arg[idx] == '\\')
                {
                    idx++;
                    numBackslash++;
                }
                if (idx == arg.Length)
                {
                    // 末尾: 閉じクオートに食われないよう倍化する。
                    sb.Append('\\', numBackslash * 2);
                }
                else if (arg[idx] == '"')
                {
                    // " の直前: 倍化 + 1 個の \ を追加して " をエスケープする。
                    sb.Append('\\', numBackslash * 2 + 1);
                    sb.Append('"');
                    idx++;
                }
                else
                {
                    sb.Append('\\', numBackslash);
                }
            }
            else if (c == '"')
            {
                sb.Append('\\');
                sb.Append('"');
            }
            else
            {
                sb.Append(c);
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
