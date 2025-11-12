using Cafe.Domain;

namespace Cafe.Application;

public class InMemoryPricingStrategyProvider : IPricingStrategyProvider
{
    private readonly IReadOnlyDictionary<PricingStrategyType, IPricingStrategy> _strategyDictionary;

    public InMemoryPricingStrategyProvider(IReadOnlyDictionary<PricingStrategyType, IPricingStrategy> strategyDictionary)
    {
        _strategyDictionary = strategyDictionary;
    }

    public IEnumerable<PricingStrategyType> StrategyTypes => _strategyDictionary.Keys;

    public Result<IPricingStrategy> Get(PricingStrategyType type)
    {
        var result = new Result<IPricingStrategy>();
        if (!_strategyDictionary.TryGetValue(type, out var strategy))
        {
            result.AddError($"Pricing strategy for type '{type}' not found.");
            return result;
        }

        result.Value = strategy;
        return result;
    }
}
