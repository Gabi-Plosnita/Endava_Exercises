namespace Cafe.Domain;

public interface IOrderEventSubscriber
{
    void OnOrderPlaced(OrderPlaced orderPlaced);
}
