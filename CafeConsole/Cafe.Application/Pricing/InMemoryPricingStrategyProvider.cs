using Cafe.Domain;

namespace Cafe.Application;

public class InMemoryPricingStrategyProvider : IPricingStrategyProvider
{
    private readonly Dictionary<string, IPricingStrategy> _strategyDictionary;

    public InMemoryPricingStrategyProvider(IEnumerable<IPricingStrategy> strategies)
    {
        _strategyDictionary = strategies.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> StrategyTypes => _strategyDictionary.Keys;

    public Result<IPricingStrategy> Get(string key)
    {
        var result = new Result<IPricingStrategy>();
        if (!_strategyDictionary.TryGetValue(key, out var strategy))
        {
            var message = ApplicationConstants.PricingStrategyNotFound(key);
            result.AddError(message);
            return result;
        }

        result.Value = strategy;
        return result;
    }
}
