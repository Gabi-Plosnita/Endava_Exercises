using Cafe.Domain;

namespace Cafe.Infrastructure;

public class AddOnDecoratorFactory : IAddOnDecoratorFactory
{
    private readonly Dictionary<string, IAddOnDecoratorRegistration> _registry;

    public AddOnDecoratorFactory(IEnumerable<IAddOnDecoratorRegistration> registrations)
    {
        _registry = registrations.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
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
