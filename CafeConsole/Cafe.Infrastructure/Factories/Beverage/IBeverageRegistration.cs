using Cafe.Domain;

namespace Cafe.Infrastructure;

public interface IBeverageRegistration
{
    string Key { get; }
    Result<IBeverage> Create();
}
