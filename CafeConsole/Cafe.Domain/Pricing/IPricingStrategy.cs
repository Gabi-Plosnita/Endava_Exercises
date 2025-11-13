namespace Cafe.Domain;

public interface IPricingStrategy
{
    string Key { get; }
    string Description { get; }
    decimal Apply(decimal cost);
}
