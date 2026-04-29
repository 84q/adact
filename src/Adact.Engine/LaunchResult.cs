namespace Adact.Engine;

/// <summary>
/// <see cref="UiaEngine.LaunchAsync"/> の結果。設計 024 §3。
/// </summary>
/// <param name="Pid">起動したプロセス ID。</param>
/// <param name="ProcessName">プロセス名 (basename + 拡張子)。UWP モードでは AUMID または fallback。</param>
/// <param name="ExecutablePath">解決済みの実行ファイルフルパス。UWP モードでは AUMID。取得不能なら null。</param>
public sealed record LaunchResult(
    int Pid,
    string ProcessName,
    string? ExecutablePath);
