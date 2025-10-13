namespace MiniBank_Console.Models.Interfaces;

public interface IInterestBearing
{
    decimal MonthlyInterestRate { get; }
    void ApplyMonthlyInterest();
}
