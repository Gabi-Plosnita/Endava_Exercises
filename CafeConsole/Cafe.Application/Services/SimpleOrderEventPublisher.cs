using Cafe.Domain;

namespace Cafe.Application;

public class SimpleOrderEventPublisher : IOrderEventPublisher
{
    private readonly List<IOrderEventSubscriber> _subscribers = new();

    public void PublishOrderPlaced(OrderPlaced orderPlaced)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnOrderPlaced(orderPlaced);
        }
    }

    public void Subscribe(IOrderEventSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
    }

    public void Unsubscribe(IOrderEventSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
    }
}
