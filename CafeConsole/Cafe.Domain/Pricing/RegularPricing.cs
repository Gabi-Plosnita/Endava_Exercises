namespace Cafe.Domain;

public class RegularPricing : IPricingStrategy
{
    public decimal GetPrice(IBeverage beverage) => beverage.Cost();
}
