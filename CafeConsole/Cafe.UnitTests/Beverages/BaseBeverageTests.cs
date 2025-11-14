using Cafe.Domain;

namespace Cafe.UnitTests;

public class BaseBeverageTests
{
    public static IEnumerable<object[]> NameTestsData()
    {
        return new List<object[]>
        {
            new object[] { new Espresso("Espresso", 2.00m), "Espresso" },
            new object[] { new HotChocolate("Hot Chocolate", 3.00m), "Hot Chocolate" },
            new object[] { new Tea("Green Tea", 1.50m), "Green Tea" }
        };
    }

    public static IEnumerable<object[]> CostTestsData()
    {
        return new List<object[]>
        {
            new object[] { new Espresso("Espresso", 2.00m), 2.00m },
            new object[] { new HotChocolate("Hot Chocolate", 3.00m), 3.00m },
            new object[] { new Tea("Green Tea", 1.50m), 1.50m }
        };
    }

    public static IEnumerable<object[]> DescribeTestsData()
    {
        return new List<object[]>
        {
            new object[] { new Espresso("Espresso", 2.00m), "Espresso" },
            new object[] { new HotChocolate("Hot Chocolate", 3.00m), "Hot Chocolate" },
            new object[] { new Tea("Green Tea", 1.50m), "Green Tea" }
        };
    }

    [Theory]
    [MemberData(nameof(NameTestsData))]
    public void Name_ReturnsGivenName_WhenCalled(IBeverage beverage, string expectedName)
    {
        var beverageName = beverage.Name;

        Assert.Equal(beverageName, expectedName);
    }

    [Theory]
    [MemberData(nameof(CostTestsData))]
    public void Cost_ReturnsBaseCost_WhenCalled(IBeverage beverage, decimal expectedCost)
    {
        var cost = beverage.Cost();

        Assert.Equal(cost, expectedCost);
    }

    [Theory]
    [MemberData(nameof(DescribeTestsData))]
    public void Describe_ReturnsName_WhenCalled(IBeverage beverage, string expectedDescription)
    {
        var description = beverage.Describe();

        Assert.Equal(description, expectedDescription);
    }
}
