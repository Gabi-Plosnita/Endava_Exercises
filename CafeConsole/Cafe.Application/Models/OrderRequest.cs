namespace Cafe.Application;

public record OrderRequest(
    string BeverageKey,
    IReadOnlyList<AddOnSelection> AddOns,
    string PricingStrategyKey
);