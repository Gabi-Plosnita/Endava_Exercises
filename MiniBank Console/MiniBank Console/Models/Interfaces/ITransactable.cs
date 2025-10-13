namespace MiniBank_Console.Models.Interfaces;

public interface ITransactable
{
    void Deposit(decimal amount);
    bool Withdraw(decimal amount, out string? error);
}
