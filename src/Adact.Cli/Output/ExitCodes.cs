namespace Adact.Cli.Output;

internal static class ExitCodes
{
  public const int Success = 0;
  public const int CommandFailed = 1;
  public const int UserError = 2;
  public const int ConnectionFailed = 3;

  /// <summary>
  /// daemon が起動できない環境を検出した場合の終了コード。
  /// 設計: discussion/018_対話セッション判定.md §5.3。
  /// </summary>
  public const int EnvironmentNotSupported = 4;
}
