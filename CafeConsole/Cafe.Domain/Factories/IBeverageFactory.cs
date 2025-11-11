namespace Cafe.Domain;

public interface IBeverageFactory
{
    IBeverage Create(string key);

    IEnumerable<string> Keys { get; }
}
