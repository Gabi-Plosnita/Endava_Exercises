namespace Cafe.Domain;

public interface IPricingStrategy
{
    decimal Apply(decimal cost);
}
