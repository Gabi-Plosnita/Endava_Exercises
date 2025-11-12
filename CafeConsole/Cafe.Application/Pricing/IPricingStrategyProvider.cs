using Cafe.Domain;

namespace Cafe.Application;

public interface IPricingStrategyProvider
{
    Result<IPricingStrategy> Get(StrategyType type);

    IEnumerable<StrategyType> StrategyTypes { get; }
}
