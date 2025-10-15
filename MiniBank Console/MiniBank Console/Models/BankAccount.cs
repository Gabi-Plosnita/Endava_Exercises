using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public abstract class BankAccount : ITransactable, IStatement
{
    private static int nextId = 1;
    public int Id { get; }
    public string Owner { get; protected set; }
    public decimal Balance { get; protected set; }
    public AccountType AccountType { get; }

    protected List<string> operationLog = new List<string>();
    public IReadOnlyList<string> OperationLog => operationLog.AsReadOnly();

    protected BankAccount(string owner, decimal balance, AccountType accountType)
    {
        Id = nextId++;
        Owner = owner;
        Balance = balance;
        AccountType = accountType;
        Log($"{AccountType} account (ID: {Id}) created for {owner} with initial balance {balance:C}");
    }

    protected BankAccount(int id, string owner, decimal balance, AccountType accountType, IEnumerable<string>? log)
    {
        Id = id;
        Owner = owner;
        Balance = balance;
        AccountType = accountType;
        if (log != null) operationLog.AddRange(log);
        if (id >= nextId) nextId = id + 1;
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

    public virtual bool Withdraw(decimal amount, out string? error)
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

    public override string ToString()
    {
        return $"Type: {AccountType} Account | ID: {Id} | Owner: {Owner} | Balance: {GetBalanceString()}";
    }

    protected void Log(string message)
    {
        operationLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    protected string GetBalanceString() => $"{(Balance < 0 ? "-" : "")}{Math.Abs(Balance):C}";
}
