namespace Cafe.Domain;

public class RegularPricing : IPricingStrategy
{
    public string Description => "Regular Pricing";

    public decimal Apply(decimal cost) => cost;
}
