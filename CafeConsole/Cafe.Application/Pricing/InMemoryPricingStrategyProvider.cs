using Cafe.Domain;

namespace Cafe.Application;

public class InMemoryPricingStrategyProvider : IPricingStrategyProvider
{
    private readonly IReadOnlyDictionary<string, IPricingStrategy> _strategyDictionary;

    public InMemoryPricingStrategyProvider(IReadOnlyDictionary<string, IPricingStrategy> strategyDictionary)
    {
        _strategyDictionary = strategyDictionary;
    }

    public IEnumerable<string> StrategyTypes => _strategyDictionary.Keys;

    public Result<IPricingStrategy> Get(string key)
    {
        var result = new Result<IPricingStrategy>();
        if (!_strategyDictionary.TryGetValue(key, out var strategy))
        {
            result.AddError($"Pricing strategy for type '{key}' not found.");
            return result;
        }

        result.Value = strategy;
        return result;
    }
}
