namespace Adact.Cli.Output;

/// <summary>
/// CLI プロセスの exit code 定数集合。設計 docs/spec/errors-and-output.md。
/// </summary>
internal static class ExitCodes
{
    /// <summary>正常終了。</summary>
    public const int Success = 0;

    /// <summary>コマンドの実行は試みたが失敗した (daemon 由来の業務エラー、内部エラー等)。</summary>
    public const int CommandFailed = 1;

    /// <summary>ユーザの引数指定に問題があり実行に至らなかった。</summary>
    public const int UserError = 2;

    /// <summary>daemon への接続そのものが失敗した。</summary>
    public const int ConnectionFailed = 3;

    /// <summary>
    /// daemon が起動できない環境を検出した場合の終了コード。
    /// 詳細は docs/spec/errors-and-output.md を参照 (背景: discussion/018_対話セッション判定.md §5.3)。
    /// </summary>
    public const int EnvironmentNotSupported = 4;
}
