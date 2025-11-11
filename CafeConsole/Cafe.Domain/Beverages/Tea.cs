namespace Cafe.Domain;

public class Tea : IBeverage
{
    private string _name;
    private decimal _baseCost;

    public Tea(string name, decimal cost)
    {
        _name = name;
        _baseCost = cost;
    }

    public string Name => _name;

    public decimal Cost() => _baseCost;

    public string Describe() => Name;
}
