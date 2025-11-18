using Cafe.Domain;

namespace Cafe.Infrastructure;

public class InMemoryOrderAnalytics : IOrderEventSubscriber
{
    public decimal Revenue { get; private set; }

    public int Count { get; private set; }

    public void OnOrderPlaced(OrderPlaced orderPlaced)
    {
        Revenue += orderPlaced.Total;
        Count++;
    }
}
