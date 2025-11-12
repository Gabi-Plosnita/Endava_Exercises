namespace Cafe.Application;

public sealed record OrderRequest(
    BeverageType BeverageType,
    IReadOnlyList<AddOnSelection> AddOns,
    StrategyType Strategy
);

