using Cafe.Domain;
using Cafe.Infrastructure;

namespace Cafe.UnitTests.Observers;

public class InMemoryOrderAnalyticsTests
{
    [Fact]
    public void OnOrderPlaced_ShouldUpdateRevenueAndCount()
    {
        var analytics = new InMemoryOrderAnalytics();
        var firstOrder = CreateOrder(3.50m);
        var secondOrder = CreateOrder(2.00m);
        var expectedRevenue = firstOrder.Total + secondOrder.Total;
        var expectedCount = 2;

        analytics.OnOrderPlaced(firstOrder);
        analytics.OnOrderPlaced(secondOrder);

        Assert.Equal(expectedRevenue, analytics.Revenue);
        Assert.Equal(expectedCount, analytics.Count);
    }

    private OrderPlaced CreateOrder(decimal total)
    {
        return new OrderPlaced(
            id: Guid.NewGuid(),
            timestamp: DateTime.UtcNow,
            beverageDescription: "Espresso x1",
            strategyDescription: "Standard Pricing",
            subtotal: total,
            total: total
        );
    }
}
