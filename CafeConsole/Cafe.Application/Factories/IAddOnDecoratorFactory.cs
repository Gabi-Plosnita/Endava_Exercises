using Cafe.Domain;

namespace Cafe.Application;

public interface IAddOnDecoratorFactory
{
    IBeverage Create(IBeverage baseBeverage, AddOnSelection selection);
}
