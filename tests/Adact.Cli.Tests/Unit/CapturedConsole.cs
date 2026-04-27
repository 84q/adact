namespace Adact.Cli.Tests.Unit;

/// <summary>
/// Console.Out / Console.Error をリダイレクトして出力をキャプチャするヘルパ。
/// </summary>
internal static class CapturedConsole
{
    public static (string stdout, string stderr) Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            action();
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
        return (outWriter.ToString(), errWriter.ToString());
    }
}
