using System.Threading;
using System.Threading.Tasks;

using Adact.Engine.Exceptions;

using FlaUI.Core.Input;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// Playwright 流のキー記述 (<c>"Ctrl+Shift+E"</c> 等) を解釈し、修飾キー押下中にメインキーを 1 回 Type する。
    /// <paramref name="refId"/> 指定時はそれにフォーカスしてから送出する。
    /// </summary>
    /// <param name="key"><c>"Ctrl+Shift+E"</c>, <c>"Enter"</c>, <c>"a"</c> などのキー記述。</param>
    /// <param name="refId">フォーカス対象の Element Ref (任意)。null の場合はウィンドウへフォーカス。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> が解析できない場合。</exception>
    /// <exception cref="RefNotFoundException"><paramref name="refId"/> が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">フォーカス / キー送出が失敗した場合。</exception>
    public Task PressAsync(string key, string? refId = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var (mods, main) = KeyParser.Parse(key);
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                if (refId is not null)
                {
                    var el = _registry.Resolve(refId);
                    el.Focus();
                }
                else
                {
                    _interaction.FocusWindow();
                }

                using (PressModifiers(mods))
                {
                    _interaction.TypeKey(main);
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId ?? "<window>", "press", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// 単一キーを Press (押下のみ。Release は呼び出し側で <see cref="KeyUpAsync"/> によって行う)。
    /// </summary>
    /// <param name="key">単一キー名。組合せ (<c>+</c> 区切り) は <see cref="ArgumentException"/>。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentException">キー記述が不正、または組合せ指定の場合。</exception>
    public Task KeyDownAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var vk = KeyParser.ParseSingle(key);
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                _interaction.PressKey(vk);
            }
            catch (Exception ex)
            {
                throw new ElementInteractionException("<window>", "key-down", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 単一キーを Release。<see cref="KeyDownAsync"/> と対で使う。
    /// </summary>
    /// <param name="key">単一キー名。組合せは不可。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentException">キー記述が不正な場合。</exception>
    public Task KeyUpAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var vk = KeyParser.ParseSingle(key);
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                _interaction.ReleaseKey(vk);
            }
            catch (Exception ex)
            {
                throw new ElementInteractionException("<window>", "key-up", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 指定要素にフォーカスし、テキストを 1 文字ずつ逐次 Type する (Playwright <c>pressSequentially</c> 相当)。
    /// </summary>
    /// <param name="refId">フォーカス対象の Element Ref。null の場合はウィンドウのフォーカスを使用する。</param>
    /// <param name="text">入力するテキスト。空文字は何もしない。</param>
    /// <param name="delayMs">各文字の間に挟むスリープ (ミリ秒)。0 以下は遅延なし。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException"><paramref name="refId"/> が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">フォーカス / キー送出が失敗した場合。</exception>
    public Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                if (refId is not null)
                {
                    var el = _registry.Resolve(refId);
                    el.Focus();
                }
                else
                {
                    _interaction.FocusWindow();
                }

                if (delayMs <= 0)
                {
                    _interaction.TypeText(text);
                }
                else
                {
                    foreach (var ch in text)
                    {
                        c.ThrowIfCancellationRequested();
                        _interaction.TypeChar(ch);
                        await Task.Delay(delayMs, c).ConfigureAwait(false);
                    }
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId ?? "<window>", "type", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }
}
