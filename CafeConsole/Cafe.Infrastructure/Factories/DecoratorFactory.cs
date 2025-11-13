using Cafe.Domain;

namespace Cafe.Infrastructure;

public class DecoratorFactory : IDecoratorFactory
{
    private readonly Dictionary<string, IDecoratorRegistration> _registry;

    public DecoratorFactory(IEnumerable<IDecoratorRegistration> regs)
    {
        _registry = regs.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> Keys => _registry.Keys;

    public Result<IBeverage> Create(string key, IBeverage inner, params object[] args)
    {
        var result = new Result<IBeverage>();
        if (!_registry.TryGetValue(key, out var registration))
        {
            result.AddError($"Unknown decorator '{key}'");
            return result;
        }

        var createDecoratorResult = registration.Create(inner, args);
        if(createDecoratorResult.IsFailure)
        {
            result.AddErrors(createDecoratorResult.Errors);
            return result;
        }
        
        result.Value = createDecoratorResult.Value;
        return result;
    }
}
