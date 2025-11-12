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

    public IPricingStrategy Get(StrategyType type)
    {
        if (!_strategyDictionary.TryGetValue(type, out var strategy))
        {
            throw new ArgumentException($"Unknown pricing strategy '{type}'.", nameof(type));
        }

        return strategy;
    }
}
