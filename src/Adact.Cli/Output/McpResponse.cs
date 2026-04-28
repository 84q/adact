using System.Text.Json;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Output;

/// <summary>
/// MCP <see cref="CallToolResult"/> から JSON を取り出す / IsError を CLI エラーとして報告する共通ヘルパ。
/// 設計 009 §5 / §6.x。
/// </summary>
internal static class McpResponse
{
  /// <summary>
  /// <see cref="CallToolResult.StructuredContent"/> を優先して取得し、なければ
  /// <see cref="CallToolResult.Content"/> 先頭の <see cref="TextContentBlock"/> を JSON parse する。
  /// </summary>
  public static JsonElement GetJson(CallToolResult result)
  {
    ArgumentNullException.ThrowIfNull(result);

    if (result.StructuredContent is JsonElement je && je.ValueKind != JsonValueKind.Undefined)
    {
      return je;
    }

    if (result.Content is { Count: > 0 } content
        && content[0] is TextContentBlock tcb
        && !string.IsNullOrEmpty(tcb.Text))
    {
      return JsonSerializer.Deserialize<JsonElement>(tcb.Text);
    }

    throw new InvalidOperationException("MCP response has neither structured content nor text content.");
  }

  /// <summary>
  /// IsError の場合、構造化された <c>{code, message, details}</c> を読み取って
  /// stderr (CliError 形式) に書き出し、対応する exit code を返す。エラーでなければ null。
  /// </summary>
  /// <remarks>
  /// 現状 daemon 側 <see cref="ToolErrors"/> でマップされるコードはすべて exit 1 (CommandFailed)。
  /// CLI 段階で検出すべき INVALID_ARGUMENT / INVALID_REF_FORMAT は呼び出し側で先にチェックする。
  /// </remarks>
  public static int? TryReportError(CallToolResult result)
  {
    ArgumentNullException.ThrowIfNull(result);
    if (result.IsError != true)
    {
      return null;
    }

    var code = ErrorCodes.InternalError;
    var message = "tool reported error";
    string? hint = null;

    try
    {
      var json = GetJson(result);
      if (json.ValueKind == JsonValueKind.Object)
      {
        if (json.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String)
        {
          code = c.GetString() ?? code;
        }
        if (json.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
        {
          message = m.GetString() ?? message;
        }
        if (json.TryGetProperty("hint", out var h) && h.ValueKind == JsonValueKind.String)
        {
          hint = h.GetString();
        }
      }
    }
    catch (JsonException)
    {
      // structured が取れなくても Content[0].Text を message として表示
      if (result.Content is { Count: > 0 } content
          && content[0] is TextContentBlock tcb
          && !string.IsNullOrEmpty(tcb.Text))
      {
        message = tcb.Text;
      }
    }
    catch (InvalidOperationException)
    {
      // 同上
      if (result.Content is { Count: > 0 } content
          && content[0] is TextContentBlock tcb
          && !string.IsNullOrEmpty(tcb.Text))
      {
        message = tcb.Text;
      }
    }

    CliError.Write(code, message, hint);
    return ExitCodes.CommandFailed;
  }
}
