using Cafe.Domain;

namespace Cafe.Infrastructure;

public interface IAddOnDecoratorRegistration
{
    string Key { get; }
    Result<IBeverage> Create(IBeverage inner, params object[] args);
}
