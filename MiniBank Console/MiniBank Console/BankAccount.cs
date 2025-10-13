namespace MiniBank_Console;

public abstract class BankAccount
{
    private static int nextId = 1;
    public int Id { get; }
    public string Owner { get; protected set; }
    public decimal Balance { get; protected set; }

    public BankAccount(string owner, decimal balance)
    {
        Id = nextId++;
        Owner = owner;
        Balance = balance;
    }

    public abstract void Deposit(decimal amount);
    public abstract bool Withdraw(decimal amount, out string? error);
}
