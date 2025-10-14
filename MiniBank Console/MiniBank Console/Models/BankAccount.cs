using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public abstract class BankAccount : ITransactable, IStatement
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

    public bool Deposit(decimal amount, out string? error)
    {
        if (amount <= 0)
        {
            error = "Deposit amount must be positive.";
            return false;
        }

        if (!CanDeposit(amount, out error))
            return false;

        Balance += amount;
        Log($"Deposited {amount:C}. New balance: {GetBalanceString()}");
        return true;
    }

    protected virtual bool CanDeposit(decimal amount, out string? error)
    {
        error = null;
        return true;
    }

    public bool Withdraw(decimal amount, out string? error)
    {
        if (amount <= 0)
        {
            error = "Withdrawal amount must be positive.";
            return false;
        }

        if (!CanWithdraw(amount, out error))
            return false;

        Balance -= amount;
        Log($"Withdrew {amount:C}. New balance: {GetBalanceString()}");
        return true;
    }

    protected abstract bool CanWithdraw(decimal amount, out string? error);

    public void PrintStatement()
    {
        Console.WriteLine($"--- Statement for {Owner} (ID: {Id}) ---");
        Console.WriteLine($"Current balance: {GetBalanceString()}");
        Console.WriteLine();

        if (operationLog.Count == 0)
            Console.WriteLine("No operations recorded yet.");
        else
        {
            foreach (var entry in operationLog)
                Console.WriteLine(entry);
        }

        Console.WriteLine("---------------------------------------\n");
    }

    protected void Log(string message)
    {
        operationLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    protected string GetBalanceString() => $"{(Balance < 0 ? "-" : "")}{Math.Abs(Balance):C}";
}
