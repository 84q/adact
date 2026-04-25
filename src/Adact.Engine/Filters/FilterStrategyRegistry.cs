using Adact.Engine.Exceptions;

namespace Adact.Engine.Filters;

/// <summary>名前 → IFilterStrategy の登録テーブル。</summary>
public sealed class FilterStrategyRegistry
{
    private readonly Dictionary<string, IFilterStrategy> _map = new(StringComparer.OrdinalIgnoreCase);

    public FilterStrategyRegistry()
    {
        Register(new OperableFilterStrategy());
        Register(new RawFilterStrategy());
    }

    public void Register(IFilterStrategy strategy)
    {
        _map[strategy.Name] = strategy;
    }

    public IFilterStrategy Get(string name)
    {
        if (_map.TryGetValue(name, out var s)) return s;
        throw new FilterStrategyNotFoundException(name);
    }

    public IReadOnlyCollection<string> Names => _map.Keys;
}
