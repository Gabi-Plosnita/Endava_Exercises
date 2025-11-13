namespace Cafe.Domain;

public class OrderPlaced
{
    public Guid Id { get; }

    public DateTime Timestamp { get; }

    public string BeverageDescription { get; }

    public decimal Subtotal { get; }

    public decimal Total { get; }

    public OrderPlaced(Guid id, DateTime timestamp, string beverageDescription, decimal subtotal, decimal total)
    {
        Id = id;
        Timestamp = timestamp;
        BeverageDescription = beverageDescription;
        Subtotal = subtotal;
        Total = total;
    }

    public OrderPlaced(IBeverage beverage, IPricingStrategy pricingStrategy)
        : this(
            Guid.NewGuid(),
            DateTime.UtcNow,
            beverage.Describe(),
            beverage.Cost(),
            pricingStrategy.Apply(beverage.Cost()))
    {
    }
}
