using Cafe.Domain;

namespace Cafe.Application;

public class SimpleOrderEventPublisher : IOrderEventPublisher
{
    private readonly IReadOnlyList<IOrderEventSubscriber> _subscribers;

    public SimpleOrderEventPublisher(IEnumerable<IOrderEventSubscriber> subscribers)
    {
        _subscribers = subscribers.ToList().AsReadOnly();
    }

    public void PublishOrderPlaced(OrderPlaced orderPlaced)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnOrderPlaced(orderPlaced);
        }
    }
}
