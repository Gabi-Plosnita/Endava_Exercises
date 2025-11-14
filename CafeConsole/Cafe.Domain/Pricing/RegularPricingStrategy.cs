namespace Cafe.Domain;

public class RegularPricingStrategy : IPricingStrategy
{
    public string Key => DomainConstants.RegularPricing;

    public string Description => DomainConstants.RegularPricingDescription;

    public decimal Apply(decimal cost) => cost;
}
