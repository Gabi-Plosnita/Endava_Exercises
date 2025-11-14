using Cafe.Domain;

namespace Cafe.UnitTests;

public class BaseBeverageTests
{
    public static IEnumerable<object[]> BeverageFactories()
    {
        return new List<object[]>
        {
            new object[] { (Func<string, decimal, IBeverage>) ((name, cost) => new Espresso(name, cost))},
            new object[] { (Func<string, decimal, IBeverage>) ((name, cost) => new HotChocolate(name, cost))},
            new object[] { (Func<string, decimal, IBeverage>) ((name, cost) => new Tea(name, cost))}
        };
    }
        

    [Theory]
    [MemberData(nameof(BeverageFactories))]
    public void Name_ReturnsGivenName_WhenCalled(Func<string, decimal, IBeverage> createBeverage)
    {
        var beverage = createBeverage("House Special", 2.50m);

        var name = beverage.Name;

        Assert.Equal("House Special", name);
    }

    [Theory]
    [MemberData(nameof(BeverageFactories))]
    public void Cost_ReturnsBaseCost_WhenCalled(Func<string, decimal, IBeverage> createBeverage)
    {
        var beverage = createBeverage("Whatever", 3.75m);

        var cost = beverage.Cost();

        Assert.Equal(3.75m, cost);
    }

    [Theory]
    [MemberData(nameof(BeverageFactories))]
    public void Describe_ReturnsName_WhenCalled(Func<string, decimal, IBeverage> createBeverage)
    {
        var beverage = createBeverage("Classic Earl Grey", 4.10m);

        var description = beverage.Describe();

        Assert.Equal("Classic Earl Grey", description);
    }
}
}
