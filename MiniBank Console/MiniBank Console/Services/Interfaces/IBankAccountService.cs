using MiniBank_Console.Models;

namespace MiniBank_Console.Services.Interfaces;

public interface IBankAccountService
{
    IReadOnlyList<BankAccount> Accounts { get; }
    bool CreateAccount(string owner, decimal balance, AccountType accountType, out string? error);
}
