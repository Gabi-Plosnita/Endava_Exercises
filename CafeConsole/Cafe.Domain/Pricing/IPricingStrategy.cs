namespace Cafe.Domain;

public interface IPricingStrategy
{
    string Key { get; }
    string Description { get; }
    decimal Calculate(decimal cost);
}
