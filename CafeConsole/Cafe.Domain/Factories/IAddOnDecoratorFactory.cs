namespace Cafe.Domain;

public interface IAddOnDecoratorFactory
{
    Result<IBeverage> Create(string key, IBeverage inner, params object[] args);

    IEnumerable<string> Keys { get; }
}
