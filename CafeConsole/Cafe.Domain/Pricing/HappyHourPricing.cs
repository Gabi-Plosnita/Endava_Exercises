namespace Cafe.Domain;

public class HappyHourPricing : IPricingStrategy
{
    private decimal _discountPercentage;

    public HappyHourPricing(decimal discountPercentage)
    {
        _discountPercentage = discountPercentage;
    }
    public decimal Apply(decimal cost)
    {
        return cost - cost * _discountPercentage;
    }
}
