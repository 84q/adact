using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Adact.Cli.Commands;

/// <summary>
/// local / serve コマンドで共有する ILoggerFactory ビルダー。stdout を汚さないよう全レベル stderr に流す。
/// </summary>
internal static class LoggerFactoryHelper
{
    /// <summary>
    /// コンソール出力を stderr に設定した <see cref="ILoggerFactory"/> を生成する。
    /// </summary>
    /// <param name="verbose">true なら Debug 以上、false なら Warning 以上を出力対象とする。</param>
    /// <returns>設定済み LoggerFactory。呼び出し側で Dispose すること。</returns>
    public static ILoggerFactory Create(bool verbose)
    {
        return LoggerFactory.Create(b =>
        {
            b.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.IncludeScopes = false;
            });
            // 全レベルを stderr に流す (stdout はデータ出力用)
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
