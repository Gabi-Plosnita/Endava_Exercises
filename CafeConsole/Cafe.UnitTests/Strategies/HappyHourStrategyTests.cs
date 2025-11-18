using Cafe.Domain;

namespace Cafe.UnitTests;

public class HappyHourStrategyTests
{
    [Theory]
    [InlineData(10.00, 8.00)]
    [InlineData(1.0, 0.8)]

    public void Calculate_ReturnsExpectedResult_WhenCalled(decimal costBeforeStrategy, decimal expectedAfterStrategy)
    {
        var strategy = new HappyHourPricingStrategy(0.2m);

        var result = strategy.Calculate(costBeforeStrategy);

        Assert.Equal(expectedAfterStrategy, result);
    }
}
