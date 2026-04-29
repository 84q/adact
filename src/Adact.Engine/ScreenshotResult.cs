namespace Adact.Engine;

/// <summary>
/// <see cref="WindowSession.ScreenshotAsync(string?, string?, CancellationToken)"/> の返却値。
/// PNG ファイルの保存パスと画像メタ情報 (幅・高さ) を保持する。設計 022 §10。
/// </summary>
/// <param name="Path">保存された PNG ファイルの絶対パス。</param>
/// <param name="Width">画像幅 (px)。</param>
/// <param name="Height">画像高さ (px)。</param>
public sealed record ScreenshotResult(string Path, int Width, int Height);
