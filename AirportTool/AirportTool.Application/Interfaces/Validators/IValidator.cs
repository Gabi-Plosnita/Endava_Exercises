namespace AirportTool.Application;

public interface IValidator<T>
{
    Result Validate(T instance);
}
