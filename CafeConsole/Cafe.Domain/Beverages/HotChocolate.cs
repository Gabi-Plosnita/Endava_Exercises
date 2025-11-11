namespace Cafe.Domain;

public class HotChocolate : IBeverage
{
    private string _name;
    private decimal _baseCost;

    public HotChocolate(string name, decimal cost)
    {
        _name = name;
        _baseCost = cost;
    }

    public string Name => _name;

    public decimal Cost() => _baseCost;

    public string Describe() => Name;
}
