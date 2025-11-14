namespace Cafe.Domain;

public class HappyHourPricing : IPricingStrategy
{
    private readonly decimal _discountPercentage;

    public HappyHourPricing(decimal discountPercentage)
    {
        _discountPercentage = discountPercentage;
    }

    public string Key => DomainConstants.HappyHourPricing;

    public string Description => DomainConstants.HappyHourPricingDescription(_discountPercentage);

    public decimal Apply(decimal cost)
    {
        return cost - cost * _discountPercentage;
    }
}
