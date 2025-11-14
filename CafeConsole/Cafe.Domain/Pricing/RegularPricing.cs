namespace Cafe.Domain;

public class RegularPricing : IPricingStrategy
{
    public string Key => DomainConstants.RegularPricing;

    public string Description => DomainConstants.RegularPricingDescription;

    public decimal Apply(decimal cost) => cost;
}
