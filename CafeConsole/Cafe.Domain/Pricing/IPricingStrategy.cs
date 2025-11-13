namespace Cafe.Domain;

public interface IPricingStrategy
{
    string Description { get; }
    decimal Apply(decimal cost);
}
