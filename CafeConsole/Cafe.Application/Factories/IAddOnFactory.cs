using Cafe.Domain;

namespace Cafe.Application;

public interface IAddOnFactory
{
    Result<IBeverage> Create(IBeverage baseBeverage, AddOnSelection selection);
}
