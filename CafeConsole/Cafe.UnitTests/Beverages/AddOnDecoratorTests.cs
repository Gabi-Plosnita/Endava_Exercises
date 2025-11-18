using Cafe.Domain;

namespace Cafe.UnitTests;

public class AddOnDecoratorTests
{
    [Fact]
    public void AddOnDecorator_InfluencesCostAndDescription()
    {
        var beverageName = "Espresso";
        var beverageBaseCost = 2.00m;
        var milkDecoratorName = "milk";
        var milkDecoratorCost = 0.50m;
        var extraShotDecoratorName = "extra shot";
        var extraShotDecoratorCost = 1.00m;
        var syrupDecoratorName = "syrup";
        var syrupFlavor = "Vanilla";
        var syrupDecoratorCost = 0.50m;
        var expectedCost = 4.00m;
        var expectedDescription = $"{beverageName}, {milkDecoratorName}, {extraShotDecoratorName}, {syrupFlavor} {syrupDecoratorName}";
        IBeverage beverage = new Espresso(beverageName, beverageBaseCost);
        beverage = new MilkDecorator(beverage, milkDecoratorName, milkDecoratorCost);
        beverage = new ExtraShotDecorator(beverage, extraShotDecoratorName, extraShotDecoratorCost);
        beverage = new SyrupDecorator(beverage, syrupDecoratorName, syrupDecoratorCost, syrupFlavor);

        decimal cost = beverage.Cost();
        string description = beverage.Describe();

        Assert.Equal(cost, expectedCost);
        Assert.Equal(description, expectedDescription);
    }
}
