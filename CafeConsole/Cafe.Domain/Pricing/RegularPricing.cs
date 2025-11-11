namespace Cafe.Domain;

public class RegularPricing : IPricingStrategy
{
    public decimal Apply(decimal cost) => cost;
}
