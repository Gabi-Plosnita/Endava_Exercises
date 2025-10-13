using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public class LoanAccount : BankAccount, IInterestBearing
{
    public LoanAccount(string owner, decimal balance) : base(owner, -balance)
    {
        Log($"Loan account (ID: {Id}) created for {owner} with initial loan amount {balance:C}");
    }

    public decimal MonthlyInterestRate => 0.01m;

    public void ApplyMonthlyInterest()
    {
        var interest = -Balance * MonthlyInterestRate;
        Balance -= interest;
        Log($"Applied interest of {interest:C}. New balance: {Balance:C}");
    }

    protected override bool CanDeposit(decimal amount, out string? error)
    {
        if(Balance + amount > 0)
        {
            error = "Deposit exceeds loan amount. Please deposit only up to the remaining loan balance.";
            return false;
        }
        error = null;
        return true;
    }

    protected override bool CanWithdraw(decimal amount, out string? error)
    {
        error = null;
        return true;
    }
}
