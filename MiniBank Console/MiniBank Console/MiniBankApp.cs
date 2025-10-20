using MiniBank_Console.Models;
using MiniBank_Console.Models.Interfaces;
using MiniBank_Console.Services.Interfaces;
using System.Globalization;

namespace MiniBank_Console;

public class MiniBankApp
{
    private readonly IBankAccountService bankAccountService;

    private bool running = true;

    public MiniBankApp(IBankAccountService bankAccountService)
    {
        this.bankAccountService = bankAccountService;
    }

    public void Run()
    {
        if(!bankAccountService.LoadAccountsFromJsonFile(Constants.AccountsJsonFilePath, out string? error))
            Console.WriteLine($"Warning: {error}");

        while (running)
        {
            ShowMenu();
            string? input = Console.ReadLine();

            switch (input)
            {
                case "1": ListAccounts(); break;
                case "2": CreateAccount(); break;
                case "3": Deposit(); break;
                case "4": Withdraw(); break;
                case "5": TransferFuds(); break;
                case "6": ViewStatement(); break;
                case "7": RunMonthEnd(); break;
                case "8": Exit(); break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private void ShowMenu()
    {
        Console.WriteLine("\n=== MINIBANK ===");
        Console.WriteLine("1. List accounts");
        Console.WriteLine("2. Create account");
        Console.WriteLine("3. Deposit");
        Console.WriteLine("4. Withdraw");
        Console.WriteLine("5. Transfer");
        Console.WriteLine("6. View statement");
        Console.WriteLine("7. Run month-end");
        Console.WriteLine("8. Exit");
        Console.WriteLine();
        Console.Write("Choose an option: ");
    }

    private void ListAccounts()
    {
        Console.WriteLine("\n--- All Accounts ---");
        var accounts = bankAccountService.GetAccounts();
        if (accounts.Count == 0)
        {
            Console.WriteLine("No accounts found.");
            return;
        }
        foreach (var account in accounts)
            Console.WriteLine(account);
    }

    private void CreateAccount()
    {
        Console.WriteLine("\n--- Create Account ---");
        Console.WriteLine("Select account type:");
        Console.WriteLine("1. Checking");
        Console.WriteLine("2. Savings");
        Console.WriteLine("3. Loan");
        Console.WriteLine("4. Fixed Deposit");
        Console.Write("Enter choice (1-4): ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > 4)
        {
            Console.WriteLine("Invalid selection. Please enter a number between 1 and 4.");
            return;
        }

        AccountType type = (AccountType)(choice - 1);

        Console.Write("Enter owner name: ");
        string? owner = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(owner))
        {
            Console.WriteLine("Owner name cannot be empty.");
            return;
        }

        Console.Write("Enter initial deposit or loan amount: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0)
        {
            Console.WriteLine("Invalid amount. Please enter a positive number.");
            return;
        }

        var bankAccountDto = new Dtos.BankAccountDto
        {
            AccountType = type,
            Owner = owner,
            Balance = amount
        };

        if (type == AccountType.FixedDeposit)
        {
            Console.Write("Enter end date (dd-MM-yyyy): ");

            if (!DateTime.TryParseExact(
                    Console.ReadLine(),
                    "dd-MM-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime endDateParsed)
                || endDateParsed <= DateTime.Now)
            {
                Console.WriteLine("Invalid end date. Please enter a valid future date in format dd-MM-yyyy.");
                return;
            }
            bankAccountDto.EndDate = endDateParsed;
        }

        if (bankAccountService.CreateAccount(bankAccountDto, out string? error))
            Console.WriteLine($"Account created successfully for {owner} with initial amount {amount:C}.");
        else
            Console.WriteLine($"Account creation failed: {error}");
    }

    private void Deposit()
    {
        Console.WriteLine("\n--- Deposit ---");

        Console.Write("Enter account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID. Please enter a number.");
            return;
        }

        var account = bankAccountService.GetAccountById(id);
        if (account == null)
        {
            Console.WriteLine($"No account found with ID {id}.");
            return;
        }

        Console.Write("Enter amount to deposit: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
        {
            Console.WriteLine("Invalid amount. Please enter a numeric value.");
            return;
        }

        if (account.Deposit(amount, out string? error))
        {
            Console.WriteLine($"Successfully deposited {amount:C} into account {id}.");
        }
        else
        {
            Console.WriteLine($"Deposit failed: {error}");
        }
    }

    private void Withdraw()
    {
        Console.WriteLine("\n--- Withdraw ---");

        Console.Write("Enter account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID. Please enter a number.");
            return;
        }

        var account = bankAccountService.GetAccountById(id);
        if (account == null)
        {
            Console.WriteLine($"No account found with ID {id}.");
            return;
        }

        Console.Write("Enter amount to withdraw: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
        {
            Console.WriteLine("Invalid amount. Please enter a numeric value.");
            return;
        }

        if (amount <= 0)
        {
            Console.WriteLine("Withdrawal amount must be positive.");
            return;
        }

        if (account.Withdraw(amount, out string? error))
        {
            Console.WriteLine($"Successfully withdrew {amount:C} from account {id}.");
        }
        else
        {
            Console.WriteLine($"Withdrawal failed: {error}");
        }
    }

    private void TransferFuds()
    {
        Console.WriteLine("\n--- Transfer Funds ---");

        Console.Write("Enter source account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fromAccountId))
        {
            Console.WriteLine("Invalid source account ID. Please enter a number.");
            return;
        }

        Console.Write("Enter destination account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int toAccountId))
        {
            Console.WriteLine("Invalid destination account ID. Please enter a number.");
            return;
        }

        Console.Write("Enter amount to transfer: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0)
        {
            Console.WriteLine("Invalid amount. Please enter a positive number.");
            return;
        }

        if (bankAccountService.TransferFunds(fromAccountId, toAccountId, amount, out string? error))
            Console.WriteLine($"Successfully transferred {amount:C} from account {fromAccountId} to {toAccountId}.");
        else
            Console.WriteLine($"Transfer failed: {error}");
    }

    private void ViewStatement()
    {
        Console.WriteLine("\n--- View Account Statement ---");

        Console.Write("Enter account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID. Please enter a numeric value.");
            return;
        }

        var account = bankAccountService.GetAccountById(id);
        if (account == null)
        {
            Console.WriteLine($"No account found with ID {id}.");
            return;
        }

        Console.WriteLine();
        account.PrintStatement();
    }

    private void RunMonthEnd()
    {
        var accounts = bankAccountService.GetAccounts();
        foreach (var account in accounts)
        {
            if(account is IInterestBearing interestBearingAccount)
                interestBearingAccount.ApplyMonthlyInterest();
        }
        Console.WriteLine("Month-end processing completed.");
    }

    private void Exit()
    {
        bankAccountService.SaveAccountsToJsonFile(Constants.AccountsJsonFilePath);
        Console.WriteLine("Thank you for using MiniBank!");
        running = false;
    }
}
