using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class LoggerFactoryHelper
{
    /// <summary>
    /// </summary>
    public static ILoggerFactory Create(bool verbose)
    {
        return LoggerFactory.Create(b =>
        {
            b.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.IncludeScopes = false;
            });
            b.Services.Configure<ConsoleLoggerOptions>(o =>
                o.LogToStandardErrorThreshold = LogLevel.Trace);
            b.AddFilter((category, level) =>
            {
                var threshold = verbose ? LogLevel.Debug : LogLevel.Warning;
                return level >= threshold;
            });
        });
    }
}
