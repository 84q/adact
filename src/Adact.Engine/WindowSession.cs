using System.Diagnostics;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Engine;

/// <summary>
/// 1 ウィンドウへの操作セッション。Snapshot / Click / Fill を提供する。
/// Session ID は <see cref="UiaEngine"/> が採番し、Detach 後も再利用しない。
/// </summary>
public sealed partial class WindowSession : IWindowSession
{
    /// <summary>UIA オートメーション。Engine と共有される (<see cref="_ownsAutomation"/> が true のときのみ Dispose する)。</summary>
    private readonly AutomationBase _automation;
    /// <summary>attach 対象の FlaUI <see cref="Window"/>。</summary>
    private readonly Window _window;
    /// <summary>snapshot の起点となる root 要素 (<see cref="_window"/> を <see cref="FlaUiElement"/> でラップ)。</summary>
    private readonly IElement _rootElement;
    /// <summary>Session スコープの Ref ID レジストリ。</summary>
    private readonly RefRegistry _registry;
    /// <summary>Engine と全 Session で共有される UIA 直列化 gate (Engine が所有し、Session は Dispose しない)。</summary>
    private readonly SemaphoreSlim _gate;
    /// <summary>本 Session のログ出力に使うロガー。</summary>
    private readonly ILogger<WindowSession> _logger;
    /// <summary>keyboard / mouse / auto-wait 操作境界。</summary>
    private readonly IWindowInteractionDriver _interaction;
    /// <summary>true のときのみ <see cref="Dispose"/> 時に <see cref="_automation"/> も Dispose する。</summary>
    private readonly bool _ownsAutomation;
    /// <summary>true のときのみ <see cref="Dispose"/> 時に <see cref="_gate"/> も Dispose する。</summary>
    private readonly bool _ownsGate;
    /// <summary>attach 時点にキャッシュした対象プロセスの PID。</summary>
    private readonly int _processId;
    /// <summary>attach 時点にキャッシュした対象プロセスの開始 UTC 時刻。取得不能時は null。</summary>
    private readonly DateTimeOffset? _processStartTimeUtc;
    /// <summary>attach 時点にキャッシュした対象プロセスの名前。</summary>
    private readonly string _processName;
    /// <summary>attach 時点にキャッシュした対象ウィンドウタイトル。</summary>
    private readonly string _title;
    /// <summary>attach 対象ウィンドウの HWND。</summary>
    private readonly nint _nativeWindowHandle;
    /// <summary>本プロセスの Windows セッション ID。操作ブロック検知用にキャッシュする。</summary>
    private readonly int _sessionId;
    /// <summary>本 Session が <see cref="Dispose"/> 済みなら 1。</summary>
    private int _disposed;

    /// <summary>
    /// 新しい <see cref="WindowSession"/> を初期化する。<see cref="UiaEngine"/> が attach 時に呼び出す。
    /// </summary>
    /// <param name="automation">UIA オートメーション (Engine と共有)。</param>
    /// <param name="window">attach 対象の FlaUI <see cref="Window"/>。</param>
    /// <param name="sessionId">Engine が採番した session ID (Ref ID の <c>s</c> 部)。</param>
    /// <param name="info">attach 時点でのウィンドウ情報スナップショット。</param>
    /// <param name="gate">Engine と全 Session で共有される UIA 直列化 gate。</param>
    /// <param name="logger">ロガー。null の場合は <see cref="NullLogger{T}"/> を使う。</param>
    /// <param name="ownsAutomation">true の場合、本 Session の Dispose 時に <paramref name="automation"/> も Dispose する。</param>
    /// <param name="ownsGate">true の場合、本 Session の Dispose 時に <paramref name="gate"/> も Dispose する。</param>
    /// <param name="rootElement">snapshot root。null の場合は <paramref name="window"/> を <see cref="FlaUiElement"/> でラップする。</param>
    /// <param name="interaction">keyboard / mouse / auto-wait 操作境界。null の場合は FlaUI 実装を使う。</param>
    internal WindowSession(
        AutomationBase automation,
        Window window,
        int sessionId,
        WindowInfo info,
        SemaphoreSlim gate,
        ILogger<WindowSession>? logger = null,
        bool ownsAutomation = false,
        bool ownsGate = false,
        IElement? rootElement = null,
        IWindowInteractionDriver? interaction = null)
    {
        _automation = automation;
        _window = window;
        _logger = logger ?? NullLogger<WindowSession>.Instance;
        _rootElement = rootElement ?? new FlaUiElement(window, _logger);
        _registry = new RefRegistry(sessionId);
        _gate = gate;
        _interaction = interaction ?? new FlaUiWindowInteractionDriver(window, info.ProcessId, _logger);
        _ownsAutomation = ownsAutomation;
        _ownsGate = ownsGate;
        _processId = info.ProcessId;
        _processStartTimeUtc = info.ProcessStartTimeUtc;
        _processName = info.ProcessName;
        _title = info.Title;
        _nativeWindowHandle = info.NativeWindowHandle;
        _sessionId = Process.GetCurrentProcess().SessionId;
    }

    /// <summary>本セッションの ID。<see cref="UiaEngine"/> が単調増加で採番し、Detach 後も再利用しない。</summary>
    public int SessionId => _registry.SessionId;

    /// <summary>attach 対象プロセスの名前 (拡張子なし)。</summary>
    public string ProcessName => _processName;

    /// <summary>attach 対象プロセスの PID。</summary>
    public int ProcessId => _processId;

    /// <summary>attach 時点での対象ウィンドウのタイトル。</summary>
    public string Title => _title;

    /// <summary>attach 対象ウィンドウの HWND。</summary>
    public nint NativeWindowHandle => _nativeWindowHandle;

    /// <summary>
    /// テスト専用: FlaUI 依存を持たない最小限の <see cref="WindowSession"/> を生成する。
    /// Snapshot / Click / Fill / Close / Kill 等の操作は呼び出してはならない (NRE になる)。
    /// </summary>
    /// <param name="sessionId">テスト用に割り当てるセッション ID。</param>
    /// <param name="info">テスト用ウィンドウ情報。</param>
    /// <returns>FlaUI 非依存の最小限な <see cref="WindowSession"/>。</returns>
    internal static WindowSession CreateForTest(int sessionId, WindowInfo info)
        => new(
            automation: null!,
            window: null!,
            sessionId: sessionId,
            info: info,
            gate: new SemaphoreSlim(1, 1),
            logger: null,
            ownsAutomation: false,
            ownsGate: true);

    /// <summary>
    /// テスト専用: FlaUI に依存しない root element を持つ最小限の <see cref="WindowSession"/> を生成する。
    /// Snapshot / Ref 解決 / IElement ベース操作の L2 テストで使用する。
    /// </summary>
    /// <param name="sessionId">テスト用に割り当てるセッション ID。</param>
    /// <param name="info">テスト用ウィンドウ情報。</param>
    /// <param name="rootElement">snapshot root になる fake element。</param>
    /// <param name="interaction">keyboard / mouse / auto-wait 操作境界。null の場合は no-op 実装を使う。</param>
    /// <returns>FlaUI 非依存の <see cref="WindowSession"/>。</returns>
    internal static WindowSession CreateForTest(
        int sessionId,
        WindowInfo info,
        IElement rootElement,
        IWindowInteractionDriver? interaction = null)
        => new(
            automation: null!,
            window: null!,
            sessionId: sessionId,
            info: info,
            gate: new SemaphoreSlim(1, 1),
            logger: null,
            ownsAutomation: false,
            ownsGate: true,
            rootElement: rootElement,
            interaction: interaction ?? new NoopWindowInteractionDriver());

    /// <summary>
    /// 対象ウィンドウの UIA ツリーを走査し、JSON snapshot を返す。Engine の直列化 gate 内で実行される。
    /// </summary>
    /// <param name="options">snapshot オプション。null の場合は既定値が使われる。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>JSON とメタ情報を含む <see cref="SnapshotResult"/>。</returns>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="SnapshotException">UIA 走査または JSON 構築に失敗した場合。</exception>
    public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var opt = options ?? new SnapshotOptions();

            var modals = DetectModalElements();
            var popups = DetectPopupElements(modals);
            _rootElement.ClearChildrenCache();
            var now = DateTimeOffset.UtcNow;
            var input = new SnapshotBuildInput(
                _rootElement, modals, popups, opt,
                WindowTitle: Title,
                ProcessName: ProcessName,
                ProcessId: ProcessId,
                GeneratedAt: now);

            try
            {
                var builder = new SnapshotBuilder(_registry);
                var built = builder.Build(input);
                return Task.FromResult(new SnapshotResult(
                    Json: built.Json,
                    SessionId: built.SessionId,
                    WindowTitle: Title,
                    ProcessName: ProcessName,
                    ProcessId: ProcessId,
                    GeneratedAt: now));
            }
            catch (Exception ex) when (ex is not AdactException)
            {
                throw new SnapshotException("Snapshot construction failed.", ex);
            }
        }, ct);
    }

    /// <summary>
    /// 指定 Ref ID の要素をクリックする。事前に対象ウィンドウへのフォーカス移動を試み、
    /// その後 InvokePattern が利用可能ならそれで invoke、不可なら FlaUI の物理クリックでフォールバックする。
    /// </summary>
    /// <param name="refId">操作対象の Ref ID。</param>
    /// <param name="options">クリックオプション (現状は将来予約)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException"><paramref name="refId"/> が現セッションで解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">UIA 操作が内部的に失敗した場合。</exception>
    public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                try { _window.Focus(); } catch (Exception ex) { _logger.LogTrace(ex, "Focus attempt failed"); }
                el.Click();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "click", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// 指定 Ref ID の入力要素にテキストをセットする。ValuePattern を優先し、不可なら Ctrl+A → Delete → Type のキー操作にフォールバックする。
    /// </summary>
    /// <param name="refId">操作対象の Ref ID。</param>
    /// <param name="text">セットするテキスト。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException"><paramref name="refId"/> が現セッションで解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">UIA 操作が内部的に失敗した場合。</exception>
    public Task FillAsync(string refId, string text, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                el.Fill(text);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "fill", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// UIA 操作を Engine と共有の gate で直列化して実行する。
    /// </summary>
    /// <typeparam name="T">アクションの戻り値の型。</typeparam>
    /// <param name="action">gate 内で実行するアクション。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns><paramref name="action"/> の実行結果。</returns>
    private async Task<T> RunSerializedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await action(ct).ConfigureAwait(false);
        }
        catch (OperationBlockedException)
        {
            throw;
        }
        catch (AdactException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var blocked = OperationBlockerDetector.Detect(_sessionId, _nativeWindowHandle);
            if (blocked.IsBlocked)
                throw new OperationBlockedException(blocked.Reason!, ex);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>戻り値なし版の <see cref="RunSerializedAsync{T}"/>。</summary>
    /// <param name="action">gate 内で実行するアクション。</param>
    /// <param name="ct">キャンセルトークン。</param>
    private async Task RunSerializedAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await action(ct).ConfigureAwait(false);
        }
        catch (OperationBlockedException)
        {
            throw;
        }
        catch (AdactException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var blocked = OperationBlockerDetector.Detect(_sessionId, _nativeWindowHandle);
            if (blocked.IsBlocked)
                throw new OperationBlockedException(blocked.Reason!, ex);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// γ 案 auto-wait: UIA WaitWhileBusy 相当として <see cref="Process.WaitForInputIdle(int)"/> を
    /// 上限 1000ms で呼び、続けて 50ms スリープする。busy 解除待機の例外は無視 (best-effort)。
    /// 詳細は 002_アーキテクチャ設計.md §6.6 参照。
    /// </summary>
    private async Task AutoWaitAfterInteractionAsync(CancellationToken ct)
        => await _interaction.WaitAfterInteractionAsync(ct).ConfigureAwait(false);

    /// <summary><see cref="Dispose"/> のエイリアス。意味的に「セッションを手放す」操作を表現する。</summary>
    public void Detach() => Dispose();

    /// <summary>
    /// UIA <c>WindowPattern.Close()</c> でウィンドウを閉じる。Pattern が取れなければ
    /// WM_CLOSE PostMessage にフォールバックする。失敗時は <see cref="CloseFailedException"/>。
    /// 成功・失敗に関わらず本メソッドはセッションの Dispose は行わない (呼び出し側が管理する)。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="CloseFailedException">WindowPattern も WM_CLOSE もウィンドウを閉じられなかった場合。</exception>
    public Task CloseAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var windowPattern = _window.Patterns.Window.PatternOrDefault;
                if (windowPattern is not null)
                {
                    windowPattern.Close();
                    return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "WindowPattern.Close() failed; falling back to WM_CLOSE");
            }

            var hwnd = _nativeWindowHandle;
            if (hwnd == IntPtr.Zero)
                throw new CloseFailedException("Window does not expose a native handle; WindowPattern was unavailable.");

            try
            {
                if (!NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
                {
                    var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    throw new CloseFailedException($"PostMessage(WM_CLOSE) failed with Win32 error {err}.");
                }
            }
            catch (CloseFailedException) { throw; }
            catch (Exception ex)
            {
                throw new CloseFailedException("Window close failed.", ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 対応プロセスを <see cref="Process.Kill(bool)"/> (entireProcessTree:true) で強制終了する。
    /// 失敗時は <see cref="KillFailedException"/>。本メソッドはセッションの Dispose は行わない。
    /// </summary>
    /// <param name="force">true の場合は WM_CLOSE をスキップし即座に Process.Kill を実行する。</param>
    /// <param name="timeoutMs">graceful shutdown の待機時間（ミリ秒）。<paramref name="force"/> が true の場合は無視される。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="KillFailedException">既にプロセスが終了している、PID 同一性検証に失敗した、または Kill が失敗した場合。</exception>
    public Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                if (_processStartTimeUtc is null)
                {
                    throw new KillFailedException(
                        $"Refusing to kill process {_processId} because the original process start time is unknown.");
                }

                using var p = Process.GetProcessById(_processId);
                DateTimeOffset currentProcessStartTimeUtc;
                try
                {
                    currentProcessStartTimeUtc = p.StartTime.ToUniversalTime();
                }
                catch (Exception ex)
                {
                    throw new KillFailedException(
                        $"Refusing to kill process {_processId} because its current start time could not be read.", ex);
                }

                if (currentProcessStartTimeUtc != _processStartTimeUtc.Value)
                {
                    throw new KillFailedException(
                        $"Refusing to kill process {_processId} because the PID now belongs to a different process.");
                }

                if (force)
                {
                    p.Kill(entireProcessTree: true);
                    return KillMethod.Forced;
                }

                // Graceful: WM_CLOSE を全トップレベルウィンドウに送信
                var pid = (uint)_processId;
                var hwnds = new List<IntPtr>();
                NativeMethods.EnumWindows((hwnd, lParam) =>
                {
#pragma warning disable CA1806 // GetWindowThreadProcessId: we only need the out param (processId)
                    NativeMethods.GetWindowThreadProcessId(hwnd, out var windowPid);
#pragma warning restore CA1806
                    if (windowPid == pid)
                        hwnds.Add(hwnd);
                    return true;
                }, IntPtr.Zero);

                foreach (var hwnd in hwnds)
                {
                    NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }

                // タイムアウト付きで終了を待機
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(c);
                timeoutCts.CancelAfter(timeoutMs);
                try
                {
                    await p.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                    return KillMethod.Graceful;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !c.IsCancellationRequested)
                {
                    // タイムアウト → 強制終了フォールバック
                    p.Kill(entireProcessTree: true);
                    return KillMethod.ForcedAfterTimeout;
                }
            }
            // プロセスが既に終了している場合も KILL_FAILED として返す (auto-detach はしない)。
            // 判断は設計 §4.5 を参照。
            catch (ArgumentException ex)
            {
                throw new KillFailedException($"Process {_processId} is no longer running.", ex);
            }
            catch (KillFailedException)
            {
                throw;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new KillFailedException($"Process.Kill failed (Win32): {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new KillFailedException($"Process.Kill failed (invalid state): {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new KillFailedException("Process kill failed.", ex);
            }
        }, ct);
    }

    /// <summary>
    /// セッションを破棄する。<c>ownsAutomation</c> が true の場合は内部 <see cref="AutomationBase"/> も Dispose する。
    /// 本メソッドは複数回呼んでも安全。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
        if (_ownsAutomation)
        {
            try { _automation.Dispose(); } catch (Exception ex) { _logger.LogTrace(ex, "Dispose failed for automation"); }
        }
        if (_ownsGate)
        {
            try { _gate.Dispose(); } catch (Exception ex) { _logger.LogTrace(ex, "Dispose failed for gate"); }
        }
    }

    /// <summary>本 Session が <see cref="Dispose"/> 済みなら <see cref="ObjectDisposedException"/> を throw する。</summary>
    /// <exception cref="ObjectDisposedException">本 Session が Dispose 済みの場合。</exception>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }

    /// <summary>
    /// このセッションのプロセスに属し、Owner がメインウィンドウでメインが WS_DISABLED な
    /// 別ウィンドウをモーダルダイアログとして検出する。
    /// </summary>
    private IReadOnlyList<IElement> DetectModalElements()
    {
        IntPtr ownerHwnd;
        try { ownerHwnd = _window.Properties.NativeWindowHandle.ValueOrDefault; }
        catch (Exception ex) { _logger.LogTrace(ex, "Failed to get NativeWindowHandle for modal detection"); return Array.Empty<IElement>(); }

        if (ownerHwnd == IntPtr.Zero) return Array.Empty<IElement>();

        // メインウィンドウが enabled なら通常ケース (モーダルがないので速攻リターンしてもよいが、
        // 念のため owner 紐付けはチェックする — ファイルダイアログ等は親が disabled)。
        bool ownerDisabled = !NativeMethods.IsWindowEnabled(ownerHwnd);

        var ownerPid = (uint)ProcessId;
        var modalHwnds = new List<IntPtr>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == ownerHwnd) return true;
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            _ = (nint)NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid != ownerPid) return true;

            var owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
            if (owner != ownerHwnd) return true;

            // Owner が disabled = 真のモーダル。enabled な場合はフローティングペインなので除外。
            if (!ownerDisabled) return true;

            modalHwnds.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        if (modalHwnds.Count == 0) return Array.Empty<IElement>();

        var result = new List<IElement>(modalHwnds.Count);
        foreach (var hwnd in modalHwnds)
        {
            try
            {
                var el = _automation.FromHandle(hwnd);
                if (el is not null) result.Add(new FlaUiElement(el, _logger));
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "Failed to wrap modal window handle {Hwnd}", hwnd);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// このセッションのプロセスに属し、メインウィンドウではなく、
    /// かつモーダルダイアログとして検出されていない可視ウィンドウを Popup として検出する。
    /// </summary>
    private IReadOnlyList<IElement> DetectPopupElements(IReadOnlyList<IElement> modalElements)
    {
        var ownerHwnd = _nativeWindowHandle;
        if (ownerHwnd == nint.Zero)
        {
            try { ownerHwnd = _window.Properties.NativeWindowHandle.ValueOrDefault; }
            catch (Exception ex) { _logger.LogTrace(ex, "Failed to get NativeWindowHandle for popup detection"); return Array.Empty<IElement>(); }
        }

        if (ownerHwnd == nint.Zero) return Array.Empty<IElement>();

        var modalHwnds = new HashSet<nint>();
        foreach (var modal in modalElements)
        {
            if (modal is FlaUiElement fe)
            {
                try
                {
                    var hwnd = fe.Inner.Properties.NativeWindowHandle.ValueOrDefault;
                    if (hwnd != nint.Zero) modalHwnds.Add(hwnd);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "Failed to get window handle for modal element");
                    }
                }
            }
        }

        var ownerPid = (uint)ProcessId;
        var popupHwnds = new List<nint>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == ownerHwnd) return true;
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            _ = (nint)NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid != ownerPid) return true;

            if (modalHwnds.Contains(hWnd)) return true;

            popupHwnds.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        if (popupHwnds.Count == 0) return Array.Empty<IElement>();

        var result = new List<IElement>(popupHwnds.Count);
        foreach (var hwnd in popupHwnds)
        {
            try
            {
                var el = _automation.FromHandle(hwnd);
                if (el is not null) result.Add(new FlaUiElement(el, _logger));
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "Failed to wrap popup window handle {Hwnd}", hwnd);
                }
            }
        }
        return result;
    }
}
