namespace Adact.Engine.Exceptions;

public sealed class FilterStrategyNotFoundException : AdactException
{
  public string Name { get; }
  public FilterStrategyNotFoundException(string name)
      : base($"Filter strategy '{name}' is not registered.")
  {
    Name = name;
  }
}
