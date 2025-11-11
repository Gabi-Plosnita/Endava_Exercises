namespace Cafe.Domain;

public interface IBeverage
{
    string Name { get; }

    decimal Cost();

    string Describe();
}