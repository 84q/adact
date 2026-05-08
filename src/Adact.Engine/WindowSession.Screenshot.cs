using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// Captures a screenshot of the window or a specific element.
    /// </summary>
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
    /// </summary>
    private string ResolveScreenshotPath(string? outPath)
    {
        if (!string.IsNullOrEmpty(outPath)) return outPath;
        var ts = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture);
        var filename = $"screenshot-{SessionId}-{ts}.png";
        return Path.Combine(".adact", filename);
    }
}
