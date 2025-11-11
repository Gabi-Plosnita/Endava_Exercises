namespace Cafe.Domain;

public class Tea : IBeverage
{
    private string _name;
    private decimal _cost;

    public Tea(string name, decimal cost)
    {
        _name = name;
        _cost = cost;
    }

    public string Name => _name;

    public decimal Cost() => _cost;

    public string Describe() => Name;
}
