using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public class CheckingAccount : BankAccount, IOverdraftPolicy
{
    public CheckingAccount(string owner, decimal amount) : base(owner, amount)
    {
        Log($"Checking account (ID: {Id}) created for {owner} with initial balance {amount:C}");
    }

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
