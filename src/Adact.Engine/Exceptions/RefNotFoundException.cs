namespace Adact.Engine.Exceptions;

public sealed class RefNotFoundException : AdactException
{
  public string RefId { get; }
  public string? Reason { get; }

  public RefNotFoundException(string refId, string? reason = null)
      : base($"Ref ID '{refId}' is not valid for this session{(reason is null ? "" : $": {reason}")}")
  {
    RefId = refId;
    Reason = reason;
  }
}
