namespace Cafe.Domain;

public interface IOrderEventPublisher
{
    void PublishOrderPlaced(OrderPlaced orderPlaced);

    void Subscribe(IOrderEventSubscriber subscriber);

    void Unsubscribe(IOrderEventSubscriber subscriber);
}
