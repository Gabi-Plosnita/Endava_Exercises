namespace Cafe.Domain;

public class Espresso : IBeverage
{
    private string _name;
    private decimal _baseCost;

    public Espresso(string name, decimal cost)
    {
        _name = name;
        _baseCost = cost;
    }

    public string Name => _name;

    public decimal Cost() => _baseCost;

    public string Describe() => Name;
}
