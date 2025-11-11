namespace Cafe.Domain;

public class ExtraShotDecorator : IBeverage
{
    private readonly IBeverage _inner;
    private readonly string _name;
    private readonly decimal _baseCost;

    public ExtraShotDecorator(IBeverage inner, string name, decimal baseCost)
    {
        _inner = inner;
        _name = name;
        _baseCost = baseCost;
    }

    public string Name => _name;

    public decimal Cost() => _inner.Cost() + _baseCost;

    public string Describe() => $"{_inner.Describe()}, {Name}";
}
