using Cafe.Domain;

namespace Cafe.Infrastructure;

public class ConsoleOrderLogger : IOrderEventSubscriber
{
    private readonly string _currency;

    public ConsoleOrderLogger(string currency)
    {
        _currency = currency;
    }

    public void OnOrderPlaced(OrderPlaced orderPlaced)
    {
        string message = $"""
            Time:{orderPlaced.Timestamp:yyyy-MM-dd HH:mm:ss}
            Description: {orderPlaced.BeverageDescription}
            Subtotal: {orderPlaced.Subtotal:0.00} {_currency}
            Pricing: {orderPlaced.StrategyDescription}
            Total: {orderPlaced.Total:0.00} {_currency}
            ----------------------------------------
            """;

        Console.WriteLine(message);
    }
}
