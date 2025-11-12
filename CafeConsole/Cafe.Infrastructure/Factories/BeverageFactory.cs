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

    public Result<IBeverage> Create(string key)
    {
        var result = new Result<IBeverage>();
        if (!_registry.TryGetValue(key, out var ctor))
        {
            result.AddError($"Unknown beverage '{key}'");
            return result;
        }

        result.Value = ctor();
        return result;
    }
}

