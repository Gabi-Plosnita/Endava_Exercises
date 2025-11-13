namespace Cafe.Domain;

public interface IDecoratorRegistration
{
    string Key { get; }
    IBeverage Create(IBeverage inner, params object[] args);
}

