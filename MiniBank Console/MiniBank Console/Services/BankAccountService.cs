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
}
