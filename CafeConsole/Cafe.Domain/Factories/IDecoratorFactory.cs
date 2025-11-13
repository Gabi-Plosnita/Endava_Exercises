namespace Cafe.Domain;

public interface IDecoratorFactory
{
    Result<IBeverage> Create(string key, IBeverage inner, params object[] args);
    IEnumerable<string> Keys { get; }
}
