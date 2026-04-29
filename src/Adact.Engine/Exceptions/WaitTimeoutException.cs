namespace Adact.Engine.Exceptions;

/// <summary>
/// <see cref="WindowSession.WaitForRefAsync"/> / <see cref="WindowSession.WaitForQueryAsync"/> /
/// <see cref="UiaEngine.WaitForWindowAsync"/> が指定タイムアウト内に成功条件を満たせなかった場合に投げる例外。
/// </summary>
public sealed class WaitTimeoutException : AdactException
{
    /// <summary>新しいインスタンスをメッセージ指定で初期化する。</summary>
    /// <param name="message">人間可読のエラーメッセージ。</param>
    public WaitTimeoutException(string message) : base(message) { }
}
