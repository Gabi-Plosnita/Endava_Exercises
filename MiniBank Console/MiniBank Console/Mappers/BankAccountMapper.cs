using MiniBank_Console.Dtos;
using MiniBank_Console.Models;

namespace MiniBank_Console.Mappers;

public static class BankAccountMapper
{
    public static BankAccountDto ToBankAccountDto(this BankAccount bankAccount) => new()
    {
        AccountType = bankAccount switch
        {
            CheckingAccount => AccountType.Checking,
            SavingsAccount => AccountType.Savings,
            LoanAccount => AccountType.Loan,
            FixedDepositAccount => AccountType.FixedDepositAccount,
            _ => throw new InvalidOperationException($"Unknown account type: {bankAccount.GetType().Name}")
        },
        Id = bankAccount.Id,
        Owner = bankAccount.Owner,
        Balance = bankAccount.Balance,
        OperationLog = bankAccount.OperationLog.ToList()
    };

    public static BankAccount ToBankAccount(this BankAccountDto dto) => dto.AccountType switch
    {
        AccountType.Checking => new CheckingAccount(dto.Id, dto.Owner, dto.Balance, dto.OperationLog),
        AccountType.Savings => new SavingsAccount(dto.Id, dto.Owner, dto.Balance, dto.OperationLog),
        AccountType.Loan => new LoanAccount(dto.Id, dto.Owner, dto.Balance, dto.OperationLog),
        AccountType.FixedDepositAccount => new FixedDepositAccount(dto.Id, dto.Owner, dto.Balance, dto.EndDate, dto.OperationLog),
        _ => throw new InvalidOperationException($"Unknown account type '{dto.AccountType}'.")
    };
}
