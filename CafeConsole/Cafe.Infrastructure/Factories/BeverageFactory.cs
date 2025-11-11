using Cafe.Domain;

namespace Cafe.Infrastructure;

public class BeverageFactory : IBeverageFactory
{
    private readonly Dictionary<string, Func<IBeverage>> _registry;

    public BeverageFactory(Dictionary<string, Func<IBeverage>> registry)
    {
        _registry = registry;
    }

    public IEnumerable<string> Keys => _registry.Keys;

    public IBeverage Create(string key)
    {
        if (!_registry.TryGetValue(key, out var ctor))
        {
            throw new ArgumentException($"Unknown beverage '{key}'");
        }

        return ctor();
    }
}

