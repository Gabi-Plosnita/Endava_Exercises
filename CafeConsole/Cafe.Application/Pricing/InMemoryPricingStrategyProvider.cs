using Cafe.Domain;

namespace Cafe.Application;

public class InMemoryPricingStrategyProvider : IPricingStrategyProvider
{
    private readonly IReadOnlyDictionary<StrategyType, IPricingStrategy> _strategyDictionary;

    public InMemoryPricingStrategyProvider(IReadOnlyDictionary<StrategyType, IPricingStrategy> strategyDictionary)
    {
        _strategyDictionary = strategyDictionary;
    }

    public IEnumerable<StrategyType> StrategyTypes => _strategyDictionary.Keys;

    public Result<IPricingStrategy> Get(StrategyType type)
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
