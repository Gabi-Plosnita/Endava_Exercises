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
            FixedDepositAccount => AccountType.FixedDeposit,
            _ => throw new InvalidOperationException($"Unknown account type: {bankAccount.GetType().Name}")
        },
        Id = bankAccount.Id,
        Owner = bankAccount.Owner,
        Balance = bankAccount.Balance,
        EndDate = bankAccount is FixedDepositAccount account ? account.EndDate : null,
        OperationLog = bankAccount.OperationLog.ToList()
    };

    public static BankAccount ToBankAccount(this BankAccountDto dto)
    {
        switch (dto.AccountType)
        {
            case AccountType.Checking:
                return new CheckingAccount(dto.Id, dto.Owner, dto.Balance, dto.OperationLog);

            case AccountType.Savings:
                return new SavingsAccount(dto.Id, dto.Owner, dto.Balance, dto.OperationLog);

            case AccountType.Loan:
                return new LoanAccount(dto.Id, dto.Owner, dto.Balance, dto.OperationLog);

            case AccountType.FixedDeposit:
                {
                    if (dto.EndDate == null)
                    {
                        throw new InvalidOperationException("End date must be a future date.");
                    }

                    return new FixedDepositAccount(dto.Id, dto.Owner, dto.Balance, dto.EndDate.Value, dto.OperationLog);
                }

            default:
                throw new InvalidOperationException($"Unknown account type '{dto.AccountType}'.");
        }
    }

}
