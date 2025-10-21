namespace MiniBank_Console.Models.Interfaces;

public interface ITransactable
{
    bool Deposit(decimal amount, out string? error);
    bool Withdraw(decimal amount, out string? error);
}
