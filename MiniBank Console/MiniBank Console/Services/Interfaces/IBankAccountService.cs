using MiniBank_Console.Dtos;
using MiniBank_Console.Models;

namespace MiniBank_Console.Services.Interfaces;

public interface IBankAccountService
{
    void SeedData();    
    IReadOnlyList<BankAccount> GetAccounts();
    BankAccount? GetAccountById(int id);
    bool CreateAccount(BankAccountDto dto, out string? error);
    bool TransferFunds(int fromAccountId, int toAccountId, decimal amount, out string? error);
    void SaveAccountsToJsonFile(string path);
    bool LoadAccountsFromJsonFile(string path, out string? error);
}
