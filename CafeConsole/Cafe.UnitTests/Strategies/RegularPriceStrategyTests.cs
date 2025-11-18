using Cafe.Domain;

namespace Cafe.UnitTests;

public class RegularPriceStrategyTests
{
    [Theory]
    [InlineData(2.99, 2.99)]
    [InlineData(10.00, 10.00)]

    public void Calculate_ReturnsExpectedResult_WhenCalled(decimal costBeforeStrategy, decimal expectedAfterStrategy)
    {
        var strategy = new RegularPricingStrategy();

        var result = strategy.Calculate(costBeforeStrategy);

        Assert.Equal(expectedAfterStrategy, result);
    }
}
