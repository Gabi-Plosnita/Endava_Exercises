using MiniBank_Console.Models;
using MiniBank_Console.Services.Interfaces;

namespace MiniBank_Console.Services;

public class BankAccountService : IBankAccountService
{
    private readonly List<BankAccount> bankAccounts = [];

    public void SeedData()
    {
        bankAccounts.Add(new CheckingAccount("Alice", 1000));
        bankAccounts.Add(new SavingsAccount("Bob", 5000));
        bankAccounts.Add(new LoanAccount("Charlie", 20000));
        bankAccounts.Add(new FixedDepositAccount("Diana", 10000, DateTime.Now.AddMonths(6)));
    }

    public IReadOnlyList<BankAccount> GetAccounts() => bankAccounts.AsReadOnly();

    public BankAccount? GetAccountById(int Id) => bankAccounts.Find(x => x.Id == Id);

    public bool CreateAccount(string owner, decimal balance, AccountType accountType, out string? error)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            error = "Owner name can't be empty!";
            return false;
        }

        if (balance < 0)
        {
            error = "Amount can't be negative";
            return false;
        }

        switch (accountType)
        {
            case AccountType.Checking:
                bankAccounts.Add(new CheckingAccount(owner, balance));
                break;

            case AccountType.Savings:
                bankAccounts.Add(new SavingsAccount(owner, balance));
                break;

            case AccountType.Loan:
                bankAccounts.Add(new LoanAccount(owner, balance));
                break;
        }

        error = null;
        return true;
    }

    public bool TransferFunds(int fromAccountId, int toAccountId, decimal amount, out string? error)
    {
        if (fromAccountId == toAccountId)
        {
            error = "Source and destination accounts cannot be the same.";
            return false;
        }

        var fromAccount = GetAccountById(fromAccountId);
        var toAccount = GetAccountById(toAccountId);

        if (fromAccount == null || toAccount == null)
        {
            error = "One or both accounts not found.";
            return false;
        }

        if (amount <= 0)
        {
            error = "Amount must be positive.";
            return false;
        }

        if (!fromAccount.Withdraw(amount, out string? withdrawError))
        {
            error = $"{withdrawError}";
            return false;
        }

        if (!toAccount.Deposit(amount, out string? depositError))
        {
            fromAccount.Deposit(amount, out _);
            error = $"{depositError}";
            return false;
        }

        error = null;
        return true;
    }
}

