using Cafe.Domain;
using Cafe.Infrastructure.Factories;

namespace Cafe.Infrastructure;

public class BeverageFactory : IBeverageFactory
{
    private readonly Dictionary<string, IBeverageRegistration> _registry;

    public BeverageFactory(IEnumerable<IBeverageRegistration> registrations)
    {
        _registry = registrations.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> Keys => _registry.Keys;

    public Result<IBeverage> Create(string key)
    {
        var result = new Result<IBeverage>();
        if (!_registry.TryGetValue(key, out var registration))
        {
            result.AddError($"Unknown beverage '{key}'");
            return result;
        }

        var createBeverageResult = registration.Create();
        result.AddErrors(createBeverageResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        result.Value = createBeverageResult.Value;
        return result;
    }
}

