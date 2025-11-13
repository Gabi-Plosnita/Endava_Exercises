using Cafe.Domain;

namespace Cafe.Infrastructure.Factories;

public interface IBeverageRegistration
{
    string Key { get; }
    Result<IBeverage> Create();
}
