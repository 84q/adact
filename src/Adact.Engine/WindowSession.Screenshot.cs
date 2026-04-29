using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// 対象ウィンドウまたは要素の bounding rect で PNG スクリーンショットを撮り、ファイルに保存する (設計 022 §10)。
    /// auto-snapshot は発火しない。
    /// </summary>
    /// <param name="refId">クリップ対象の Element Ref。<c>null</c> ならウィンドウ全体を撮る。</param>
    /// <param name="outPath">出力先ファイルパス。<c>null</c> なら <c>.adact/screenshot-&lt;sid&gt;-&lt;UTC ts&gt;.png</c>。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>保存パス (絶対パス) と画像メタ情報を含む <see cref="ScreenshotResult"/>。</returns>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException"><paramref name="refId"/> が現セッションで解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">bounding rect が不正でキャプチャできない場合。</exception>
    public Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();

            Rect rect;
            string opName;
            if (refId is null)
            {
                opName = "screenshot";
                rect = WindowBoundingRect();
            }
            else
            {
                opName = "screenshot";
                var el = _registry.Resolve(refId);
                rect = el.BoundingRectangle;
            }

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                throw new ElementInteractionException(refId ?? string.Empty, opName,
                    $"target bounding rectangle is empty ({rect.Width}x{rect.Height}); element may be minimized or offscreen.");
            }

            var resolvedPath = ResolveScreenshotPath(outPath);
            var dir = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            try
            {
                using var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(rect.X, rect.Y, 0, 0,
                        new Size(rect.Width, rect.Height), CopyPixelOperation.SourceCopy);
                }
                bitmap.Save(resolvedPath, ImageFormat.Png);
                return Task.FromResult(new ScreenshotResult(
                    Path: Path.GetFullPath(resolvedPath),
                    Width: rect.Width,
                    Height: rect.Height));
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId ?? string.Empty, opName, ex.Message, ex);
            }
        }, ct);
    }

    /// <summary>FlaUI <c>Window</c> の BoundingRectangle を <see cref="Rect"/> に変換する (取得失敗時は既定値)。</summary>
    /// <returns>ウィンドウの bounding rect。</returns>
    private Rect WindowBoundingRect()
    {
        try
        {
            var r = _window.Properties.BoundingRectangle.ValueOrDefault;
            return new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// <paramref name="outPath"/> が指定されていればそれを返す。null/空なら <c>.adact/screenshot-&lt;sid&gt;-&lt;ts&gt;.png</c> を生成する。
    /// </summary>
    /// <param name="outPath">CLI/MCP から渡された出力パス。</param>
    /// <returns>解決後の保存先パス (相対パスのまま返す可能性がある)。</returns>
    private string ResolveScreenshotPath(string? outPath)
    {
        if (!string.IsNullOrEmpty(outPath)) return outPath;
        var ts = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var filename = $"screenshot-{SessionId}-{ts}.png";
        return Path.Combine(".adact", filename);
    }
}
