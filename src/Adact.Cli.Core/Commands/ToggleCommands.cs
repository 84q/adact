using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>check</c> コマンド。Toggle/Selection 系要素を On 状態にする (auto-snapshot あり、idempotent)。</summary>
internal static class CheckCommand
{
    /// <summary>check サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "check",
        description: "Ensure a checkbox / toggle / radio is in the On state. Idempotent.",
        toolName: "windows_check",
        autoSnapshot: true);
}

/// <summary><c>uncheck</c> コマンド。Toggle 系要素を Off 状態にする (auto-snapshot あり、idempotent)。</summary>
internal static class UncheckCommand
{
    /// <summary>uncheck サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "uncheck",
        description: "Ensure a checkbox / toggle is in the Off state. Idempotent.",
        toolName: "windows_uncheck",
        autoSnapshot: true);
}

/// <summary><c>focus</c> コマンド。指定要素にキーボードフォーカスを当てる (低レベル)。</summary>
internal static class FocusCommand
{
    /// <summary>focus サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "focus",
        description: "Set keyboard focus to the element identified by ref.",
        toolName: "windows_focus",
        autoSnapshot: false);
}

/// <summary><c>clear</c> コマンド。入力要素の値を空文字列でクリアする (auto-snapshot あり)。</summary>
internal static class ClearCommand
{
    /// <summary>clear サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "clear",
        description: "Clear the value of an input element (windows_fill with empty string).",
        toolName: "windows_clear",
        autoSnapshot: true);
}

/// <summary><c>scroll</c> コマンド。ScrollItemPattern で要素を可視範囲にスクロールする (低レベル)。</summary>
internal static class ScrollIntoViewCommand
{
    /// <summary>scroll サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build() => RefOnlyCommandBuilder.Build(
        name: "scroll",
        description: "Scroll the element into view using ScrollItemPattern.",
        toolName: "windows_scroll_into_view",
        autoSnapshot: false);
}
