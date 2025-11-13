using Cafe.Domain;

namespace Cafe.Infrastructure;

public interface IDecoratorRegistration
{
    string Key { get; }
    IBeverage Create(IBeverage inner, params object[] args);
}
