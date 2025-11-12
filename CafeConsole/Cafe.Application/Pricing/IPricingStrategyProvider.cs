using Cafe.Domain;

namespace Cafe.Application;

public interface IPricingStrategyProvider
{
    Result<IPricingStrategy> Get(PricingStrategyType type);

    IEnumerable<PricingStrategyType> StrategyTypes { get; }
}
