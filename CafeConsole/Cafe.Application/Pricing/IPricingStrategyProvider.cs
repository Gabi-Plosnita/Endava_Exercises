using Cafe.Domain;

namespace Cafe.Application;

public interface IPricingStrategyProvider
{
    IPricingStrategy Get(StrategyType type);

    IEnumerable<StrategyType> StrategyTypes { get; }
}
