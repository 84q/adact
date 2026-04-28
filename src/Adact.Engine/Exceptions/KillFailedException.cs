namespace Adact.Engine.Exceptions;

/// <summary>Process.Kill() 経由でプロセス終了が失敗した。</summary>
public sealed class KillFailedException : AdactException
{
  public KillFailedException(string message) : base(message) { }
  public KillFailedException(string message, Exception inner) : base(message, inner) { }
}
