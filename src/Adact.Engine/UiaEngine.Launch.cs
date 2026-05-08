using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Adact.Engine.Exceptions;

using Microsoft.Extensions.Logging;

namespace Adact.Engine;

public sealed partial class UiaEngine
{
    private const string UwpPrefix = "shell:AppsFolder\\";

    /// <summary>
    /// Launches a Win32 app or UWP app by executable or AUMID.
    /// </summary>
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
            _logger.LogDebug(ex, "Failed to read MainModule.FileName for pid {Pid}; returning null", pid);
        }

        return new LaunchResult(pid, processName, executablePath);
    }

    private LaunchResult LaunchUwp(string aumid, IReadOnlyList<string>? arguments)
    {
        if (string.IsNullOrWhiteSpace(aumid))
        {
            throw new LaunchFailedException("UWP launch requires a non-empty AUMID after 'shell:AppsFolder\\'.");
        }

        var argString = arguments is { Count: > 0 }
            ? string.Join(' ', arguments.Select(QuoteIfNeeded))
            : string.Empty;

        try
        {
            var clsid = NativeMethods.CLSID_ApplicationActivationManager;
            var type = Type.GetTypeFromCLSID(clsid)
                ?? throw new LaunchFailedException(
                    "ApplicationActivationManager COM class is not registered on this system.");
            var comObject = Activator.CreateInstance(type)
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
    }

    /// <summary>
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/PasteArguments.cs
    /// </summary>
    /// <remarks>
    /// </remarks>
    internal static string QuoteIfNeeded(string arg)
    {
        if (arg is null) return "\"\"";
        if (arg.Length != 0 && arg.IndexOfAny([' ', '\t', '"']) < 0)
        {
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
                    sb.Append('\\', numBackslash * 2);
                }
                else if (arg[idx] == '"')
                {
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
