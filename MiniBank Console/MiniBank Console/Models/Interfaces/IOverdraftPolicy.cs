namespace MiniBank_Console.Models.Interfaces;

public interface IOverdraftPolicy
{
    decimal OverdraftLimit { get; }
}
