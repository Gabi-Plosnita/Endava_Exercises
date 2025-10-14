using MiniBank_Console.Models;

namespace MiniBank_Console.Services.Interfaces;

public interface IBankAccountService
{
    void SeedData();    
    IReadOnlyList<BankAccount> GetAccounts();
    BankAccount? GetAccountById(int id);
    bool CreateAccount(string owner, decimal balance, AccountType accountType, out string? error);
    bool TransferFunds(int fromAccountId, int toAccountId, decimal amount, out string? error);
}
