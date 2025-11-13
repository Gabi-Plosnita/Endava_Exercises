using Cafe.Domain;

namespace Cafe.Application;

public interface IAddOnDecoratorFactory
{
    Result<IBeverage> Create(IBeverage baseBeverage, AddOnSelection selection);
}
