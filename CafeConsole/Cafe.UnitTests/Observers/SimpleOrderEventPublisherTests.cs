using Cafe.Application;
using Cafe.Domain;
using Moq;

namespace Cafe.UnitTests;

public class SimpleOrderEventPublisherTests
{
    [Fact]
    public void PublishOrderPlaced_CallsSubscriberOnce_WithMatchingTotals()
    {
        var mockSubscriber = new Mock<IOrderEventSubscriber>(MockBehavior.Strict);
        var order = CreateOrder();

        mockSubscriber
            .Setup(s => s.OnOrderPlaced(It.Is<OrderPlaced>(o =>
                o.Subtotal == order.Subtotal &&
                o.Total == order.Total)))
            .Verifiable();

        var publisher = new SimpleOrderEventPublisher(new[] { mockSubscriber.Object });

        publisher.PublishOrderPlaced(order);

        mockSubscriber.Verify(
            s => s.OnOrderPlaced(It.Is<OrderPlaced>(o =>
                o.Subtotal == order.Subtotal &&
                o.Total == order.Total)),
            Times.Once);

        mockSubscriber.VerifyNoOtherCalls();
    }

    private OrderPlaced CreateOrder()
    {
        return new OrderPlaced(
            id: Guid.NewGuid(),
            timestamp: DateTime.UtcNow,
            beverageDescription: "Latte x1",
            strategyDescription: "Standard Pricing",
            subtotal: 4.50m,
            total: 4.50m
        );
    }
}
