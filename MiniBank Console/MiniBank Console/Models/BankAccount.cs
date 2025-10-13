using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public abstract class BankAccount : ITransactable
{
    private static int nextId = 1;
    public int Id { get; }
    public string Owner { get; protected set; }
    public decimal Balance { get; protected set; }

    protected List<string> operationLog = new List<string>();
    public IReadOnlyList<string> OperationLog => operationLog.AsReadOnly();

    public BankAccount(string owner, decimal balance)
    {
        Id = nextId++;
        Owner = owner;
        Balance = balance;
    }

    public abstract void Deposit(decimal amount);
    public abstract bool Withdraw(decimal amount, out string? error);
}
