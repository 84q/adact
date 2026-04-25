namespace Adact.Engine.Exceptions;

/// <summary>ADACT で発生するすべての独自例外の基底。</summary>
public abstract class AdactException : Exception
{
    protected AdactException(string message) : base(message) { }
    protected AdactException(string message, Exception inner) : base(message, inner) { }
}
