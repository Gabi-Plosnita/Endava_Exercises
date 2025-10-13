using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public class SavingsAccount : BankAccount, IInterestBearing
{
    public SavingsAccount(string owner, decimal amount) : base(owner, amount)
    {
        Log($"Savings account (ID: {Id}) created for {owner} with initial balance {amount:C}");
    }

    public decimal MonthlyInterestRate => 0.01m; 

    public void ApplyMonthlyInterest()
    {
        var interest = Balance * MonthlyInterestRate;
        Balance += interest;
        Log($"Applied interest of {interest:C}. New balance: {Balance:C}");
    }

    protected override bool CanWithdraw(decimal amount, out string? error)
    {
        if (amount > Balance)
        {
            error = "Insufficient funds for withdrawal.";
            return false;
        }
        error = null;
        return true;
    }
}
