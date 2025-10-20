using MiniBank_Console.Models.Interfaces;

namespace MiniBank_Console.Models;

public class FixedDepositAccount : BankAccount, IInterestBearing
{
    public DateTime EndDate { get; }

    private const decimal PenaltyRate = 0.05m;

    public FixedDepositAccount(string owner, decimal amount, DateTime endDate)
        : base(owner, amount, AccountType.FixedDeposit)
    {
        EndDate = endDate;
    }
    public FixedDepositAccount(int id, string owner, decimal amount, DateTime endDate, IEnumerable<string>? log)
        : base(id, owner, amount, AccountType.FixedDeposit, log) 
    {
        EndDate = endDate;
    }

    public decimal MonthlyInterestRate => 0.01m;

    public void ApplyMonthlyInterest()
    {
        var interest = Balance * MonthlyInterestRate;
        Balance += interest;
        Log($"Applied interest of {interest:C}. New balance: {Balance:C}");
    }

    protected override bool CanDeposit(decimal amount, out string? error)
    {
        error = "Deposits are not allowed in Fixed Deposit Accounts.";
        return false;
    }

    public override bool Withdraw(decimal amount, out string? error)
    {
        if (!base.Withdraw(amount, out error))
        {
            return false;
        }

        if (DateTime.Now < EndDate)
        {
            var penalty = amount * PenaltyRate;
            Balance -= penalty;
            Log($"Early withdrawal penalty charged: {penalty:C}. New balance: {Balance:C}");
        }

        return true;
    }

    protected override bool CanWithdraw(decimal amount, out string? error)
    {
        if (DateTime.Now < EndDate)
        {
            var penalty = amount * PenaltyRate;
            var totalDeduction = amount + penalty;
            if (totalDeduction > Balance)
            {
                error = $"Insufficient funds for withdrawal and penalty. Penalty is {penalty:C}.";
                return false;
            }
        }
        else if (amount > Balance)
        {
            error = "Insufficient funds for withdrawal.";
            return false;
        }
        error = null;
        return true;
    }
}
