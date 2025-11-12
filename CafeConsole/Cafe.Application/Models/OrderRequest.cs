namespace Cafe.Application;

public record OrderRequest(
    BeverageType BeverageType,
    IReadOnlyList<AddOnSelection> AddOns,
    StrategyType Strategy
);

