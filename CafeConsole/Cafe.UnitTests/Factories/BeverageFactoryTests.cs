using Cafe.Domain;
using Cafe.Infrastructure;

namespace Cafe.UnitTests;

public class BeverageFactoryTests
{
    public static IEnumerable<object[]> BeverageFactory_TestsData()
    {
        return new List<object[]>
        {
            new object[] { new EspressoRegistration("Espresso", 2.00m), "Espresso", typeof(Espresso), 2.00m },
            new object[] { new HotChocolateRegistration("Hot Chocolate", 3.00m), "Hot Chocolate", typeof(HotChocolate), 3.00m },
            new object[] { new TeaRegistration("Green Tea", 1.50m), "Green Tea", typeof(Tea), 1.50m },
        };
    }

    [Theory]
    [MemberData(nameof(BeverageFactory_TestsData))]
    public void BeverageFactory_Create_ReturnsExpectedBeverage(
        IBeverageRegistration registration, string key, Type expectedType, decimal expectedCost)
    {
        var registrations = new List<IBeverageRegistration> { registration };
        var factory = new BeverageFactory(registrations);

        var beverageResult = factory.Create(key);
        var beverage = beverageResult.Value;

        Assert.True(beverageResult.IsSuccessful);
        Assert.NotNull(beverage);
        Assert.IsType(expectedType, beverage);
        Assert.Equal(key, beverage.Name);
        Assert.Equal(expectedCost, beverage.Cost());
    }

    [Fact]
    public void BeverageFactory_Create_ReturnsErrorWhenUnknownBeverageKey()
    {
        var registrations = new List<IBeverageRegistration>
        {
            new EspressoRegistration("Espresso", 2.00m),
            new HotChocolateRegistration("Hot Chocolate", 3.00m),
            new TeaRegistration("Green Tea", 1.50m)
        };
        var factory = new BeverageFactory(registrations);

        var beverageResult = factory.Create("Inexistent Key");

        Assert.True(beverageResult.IsFailure);
        Assert.Null(beverageResult.Value);
    }
}
