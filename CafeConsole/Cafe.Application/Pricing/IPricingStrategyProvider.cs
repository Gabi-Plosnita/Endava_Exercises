using Cafe.Domain;

namespace Cafe.Application;

public interface IPricingStrategyProvider
{
    Result<IPricingStrategy> Get(string key);

    IEnumerable<string> StrategyTypes { get; }
}
