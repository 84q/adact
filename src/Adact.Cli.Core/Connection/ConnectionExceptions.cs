namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal sealed class InvalidUrlException : Exception
{
    public InvalidUrlException(string message) : base(message) { }
}

/// <summary>
/// </summary>
internal sealed class ConfigParseException : Exception
{
    public ConfigParseException(string message, Exception? inner = null) : base(message, inner) { }
}
