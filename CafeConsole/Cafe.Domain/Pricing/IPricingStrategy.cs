namespace Cafe.Domain;

public interface IPricingStrategy
{
    decimal GetPrice(IBeverage beverage);
}
