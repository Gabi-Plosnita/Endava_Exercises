namespace Cafe.Domain;

public class HappyHourPricing : IPricingStrategy
{
    private readonly decimal _discountPercentage;

    public HappyHourPricing(decimal discountPercentage)
    {
        _discountPercentage = discountPercentage;
    }

    public string Key => "Happy Hour";

    public string Description => $"Happy Hour ({_discountPercentage:P0})";

    public decimal Apply(decimal cost)
    {
        return cost - cost * _discountPercentage;
    }
}
