using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public class CheckingAccount : BankAccount, IOverdraftPolicy
{
    public CheckingAccount(string owner, decimal amount) 
        : base(owner, amount, AccountType.Checking) { }
    public CheckingAccount(int id, string owner, decimal amount, IEnumerable<string>? log)
        : base(id, owner, amount, AccountType.Checking, log) { }

    public decimal OverdraftLimit => 200;

    protected override bool CanWithdraw(decimal amount, out string? error)
    {
        if(Balance - amount < -OverdraftLimit)
        {
            error = $"Withdrawal exceeds overdraft limit of {OverdraftLimit:C}.";
            return false;
        }
        error = null;
        return true;
    }
}
