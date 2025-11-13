namespace Cafe.Application;

public record OrderRequest(
    string BeverageType,
    IReadOnlyList<AddOnSelection> AddOns,
    string PricingStrategyKey
);