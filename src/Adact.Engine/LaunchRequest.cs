namespace Adact.Engine;

/// <summary>
/// <see cref="UiaEngine.LaunchAsync"/> に渡す起動要求。設計 024 §3。
/// </summary>
/// <param name="Executable">実行ファイルパス、PATH 探索対象の名前、もしくは UWP の <c>shell:AppsFolder\&lt;AUMID&gt;</c> 形式。</param>
/// <param name="Arguments">コマンドライン引数 (各要素は 1 引数として渡される)。空または null は無引数。</param>
/// <param name="WorkingDirectory">作業ディレクトリ。null/空は呼び出し元プロセスの cwd を継承。UWP モードでは指定不可。</param>
/// <param name="Environment">環境変数の上書き / 追加。null/空は継承のみ。UWP モードでは指定不可。</param>
public sealed record LaunchRequest(
    string Executable,
    IReadOnlyList<string>? Arguments = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null);
