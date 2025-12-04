namespace Cafe.Domain;

public interface IBeverageFactory
{
    Result<IBeverage> Create(string key);

    IEnumerable<string> Keys { get; }
}
