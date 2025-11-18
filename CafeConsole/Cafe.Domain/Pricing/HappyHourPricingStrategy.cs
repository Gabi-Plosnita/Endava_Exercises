namespace Cafe.Domain;

public class HappyHourPricingStrategy : IPricingStrategy
{
    private readonly decimal _discountPercentage;

    public HappyHourPricingStrategy(decimal discountPercentage)
    {
        _discountPercentage = discountPercentage;
    }

    public string Key => DomainConstants.HappyHourPricing;

    public string Description => DomainConstants.HappyHourPricingDescription(_discountPercentage);

    public decimal Calculate(decimal cost)
    {
        return cost - cost * _discountPercentage;
    }
}
