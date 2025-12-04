namespace Cafe.Domain;

public interface IOrderEventPublisher
{
    void PublishOrderPlaced(OrderPlaced orderPlaced);
}
