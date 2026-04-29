using System.Diagnostics;

using Adact.Engine.Exceptions;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Engine;

/// <summary>
/// ADACT Engine のエントリポイント。ウィンドウ列挙とアタッチを担当する。
/// 内部に <see cref="UIA3Automation"/> を 1 個だけ保持し、Session 間で共有する。
/// </summary>
public sealed class UiaEngine : IDisposable
{
    /// <summary>本 Engine が保持する UIA オートメーション。Session 間で共有する。</summary>
    private readonly AutomationBase _automation;
    /// <summary>Engine と Session 用ロガー生成に使う <see cref="ILoggerFactory"/>。</summary>
    private readonly ILoggerFactory _loggerFactory;
    /// <summary>本 Engine 自身のログ出力に使うロガー。</summary>
    private readonly ILogger<UiaEngine> _logger;
    /// <summary>
    /// UIA 操作のマシン内直列化 gate。UIA はマシン全体で前面ウィンドウを取り合うため、
    /// Engine と Engine が払い出す WindowSession の UIA 操作はこの 1 本で直列化する。
    /// Engine と全 Session で同じインスタンスを共有する。詳細は 006_Phase4_設計.md §5 参照。
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>次に払い出すセッション ID。<see cref="Interlocked.Increment(ref int)"/> で単調増加採番する。</summary>
    private int _nextSessionId;
    /// <summary>本 Engine が <see cref="Dispose"/> 済みであれば true。</summary>
    private bool _disposed;

    /// <summary>標準の <see cref="UIA3Automation"/> を内部で生成する production 用コンストラクタ。</summary>
    /// <param name="loggerFactory">ログ出力に使用するロガーファクトリ。null の場合は <see cref="NullLoggerFactory"/> を使う。</param>
    public UiaEngine(ILoggerFactory? loggerFactory = null)
        : this(new UIA3Automation(), loggerFactory)
    {
    }

    /// <summary>
    /// テスト容易性および将来の UIA2 fallback のため、外部から <see cref="AutomationBase"/> を注入可能。
    /// 渡された automation は本インスタンスが Dispose する。
    /// </summary>
    /// <param name="automation">注入する <see cref="AutomationBase"/> 実装。</param>
    /// <param name="loggerFactory">ログ出力に使用するロガーファクトリ。null の場合は <see cref="NullLoggerFactory"/> を使う。</param>
    internal UiaEngine(AutomationBase automation, ILoggerFactory? loggerFactory = null)
    {
        _automation = automation;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<UiaEngine>();
    }

    /// <summary>
    /// 現在のデスクトップ上の可視トップレベルウィンドウを列挙する。
    /// 不可視・オフスクリーンの UWP CoreWindow 等は除外し、HWND ベースで重複も排除する。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>列挙されたウィンドウ情報のリスト。</returns>
    /// <exception cref="ObjectDisposedException">本インスタンスが Dispose 済みの場合。</exception>
    public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync<IReadOnlyList<WindowInfo>>(c =>
        {
            c.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<WindowInfo>>(ListWindowsCore());
        }, ct);
    }

    /// <summary>
    /// gate 内で同期的に現デスクトップのトップレベルウィンドウを列挙する内部実装。
    /// 同 gate 内 (例: <see cref="AttachByHandleAsync"/>) からの再呼び出しで self-deadlock しないよう
    /// 公開 API ではなくこちらを直接呼ぶ。
    /// </summary>
    /// <returns>列挙されたウィンドウ情報のリスト。</returns>
    private List<WindowInfo> ListWindowsCore()
    {
        var desktop = _automation.GetDesktop();
        var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
        var list = new List<WindowInfo>(windows.Length);
        var seenHwnds = new HashSet<nint>();
        foreach (var w in windows)
        {
            try
            {
                var hwnd = w.Properties.NativeWindowHandle.ValueOrDefault;
                // 可視 & オンスクリーンのみを採用 (UWP の隠れた CoreWindow を除外)
                if (hwnd == IntPtr.Zero) continue;
                if (!NativeMethods.IsWindowVisible(hwnd)) continue;
                if (w.Properties.IsOffscreen.ValueOrDefault) continue;
                if (!seenHwnds.Add(hwnd)) continue;

                var pid = w.Properties.ProcessId.ValueOrDefault;
                var procName = "?";
                try { procName = Process.GetProcessById(pid).ProcessName; } catch { }
                var title = w.Properties.Name.ValueOrDefault ?? "";
                var ctrl = SafeControlType(w);
                var className = w.Properties.ClassName.ValueOrDefault;
                list.Add(new WindowInfo(pid, procName, title, ctrl, NullIfEmpty(className), hwnd));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read a window during ListWindowsAsync; skipping.");
            }
        }
        return list;
    }

    /// <summary>
    /// HWND 直指定で attach する。HWND 一致で 1 件確定する。
    /// 該当 HWND が現在の列挙に含まれない、もしくは <c>FromHandle</c> が失敗した場合は
    /// <see cref="WindowNotFoundException"/> を throw する。
    /// </summary>
    /// <param name="hwnd">attach 対象の Win32 ウィンドウハンドル。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>新規に生成された <see cref="WindowSession"/>。</returns>
    /// <exception cref="ObjectDisposedException">本インスタンスが Dispose 済みの場合。</exception>
    /// <exception cref="WindowNotFoundException">HWND が現在の列挙に存在しない、または UIA から再取得できなかった場合。</exception>
    public Task<WindowSession> AttachByHandleAsync(nint hwnd, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var all = ListWindowsCore();
            var target = all.FirstOrDefault(w => w.NativeWindowHandle == hwnd);
            if (target is null)
                throw new WindowNotFoundException(hwnd);

            AutomationElement? raw;
            try
            {
                raw = _automation.FromHandle(target.NativeWindowHandle);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "FromHandle failed for hwnd {Hwnd}", target.NativeWindowHandle);
                }
                throw new WindowNotFoundException(hwnd);
            }
            if (raw is null)
                throw new WindowNotFoundException(hwnd);

            var sessionId = Interlocked.Increment(ref _nextSessionId);
            var session = new WindowSession(
                _automation,
                raw.AsWindow(),
                sessionId,
                target,
                _gate,
                _loggerFactory.CreateLogger<WindowSession>(),
                ownsAutomation: false);
            return Task.FromResult(session);
        }, ct);
    }

    /// <summary>
    /// UIA 操作を直列化して実行する。Engine と Engine から払い出された全 WindowSession の
    /// UIA 操作はこの gate を共有する。
    /// </summary>
    /// <typeparam name="T">action の戻り型。</typeparam>
    /// <param name="action">gate 内で実行するアクション。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>action の戻り値。</returns>
    internal async Task<T> RunSerializedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await action(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 戻り値なし版の <see cref="RunSerializedAsync{T}"/>。
    /// </summary>
    /// <param name="action">gate 内で実行するアクション。</param>
    /// <param name="ct">キャンセルトークン。</param>
    internal async Task RunSerializedAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await action(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// <see cref="AutomationElement.ControlType"/> の取得が UIA エラーで失敗しても列挙を止めないよう、
    /// 例外を握り潰して "Unknown" を返すヘルパ。
    /// </summary>
    /// <param name="el">対象要素。</param>
    /// <returns>ControlType の文字列、取得失敗時は "Unknown"。</returns>
    private static string SafeControlType(AutomationElement el)
    {
        try { return el.ControlType.ToString(); } catch { return "Unknown"; }
    }

    /// <summary>空文字列を <c>null</c> に正規化する。</summary>
    /// <param name="s">入力文字列。</param>
    /// <returns><paramref name="s"/> が <c>null</c> または空文字列なら <c>null</c>、それ以外はそのまま返す。</returns>
    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>
    /// 内部の <see cref="AutomationBase"/> と直列化 gate を破棄する。本メソッドは複数回呼んでも安全。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _automation.Dispose(); } catch { }
        try { _gate.Dispose(); } catch { }
    }

    /// <summary>本 Engine が <see cref="Dispose"/> 済みなら <see cref="ObjectDisposedException"/> を throw する。</summary>
    /// <exception cref="ObjectDisposedException">本 Engine が Dispose 済みの場合。</exception>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
