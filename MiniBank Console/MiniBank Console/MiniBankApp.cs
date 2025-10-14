using MiniBank_Console.Models;
using MiniBank_Console.Services.Interfaces;

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
        while (running)
        {
            ShowMenu();
            Console.Write("Choose an option: ");
            string? input = Console.ReadLine();

            switch (input)
            {
                case "1": ListAccounts(); break;
                case "2": CreateAccount(); break;
                case "3": Deposit(); break;
                case "4": Withdraw(); break;
                case "5": ViewStatement(); break;
                case "6": RunMonthEnd(); break;
                case "7": Exit(); break;
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
        Console.WriteLine("5. View statement");
        Console.WriteLine("6. Run month-end");
        Console.WriteLine("7. Exit");
        Console.WriteLine();
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
        {
            Console.WriteLine(account);
        }
    }

    private void CreateAccount()
    {
        Console.WriteLine("\n--- Create Account ---");
        Console.WriteLine("Select account type:");
        Console.WriteLine("1. Checking");
        Console.WriteLine("2. Savings");
        Console.WriteLine("3. Loan");
        Console.Write("Enter choice (1-3): ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > 3)
        {
            Console.WriteLine("Invalid selection. Please enter a number between 1 and 3.");
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

        if (bankAccountService.CreateAccount(owner, amount, type, out string? error))
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
            if (account is SavingsAccount savingsAccount)
                savingsAccount.ApplyMonthlyInterest();
            else if(account is LoanAccount loanAccount)
                loanAccount.ApplyMonthlyInterest();
        }
        Console.WriteLine("Month-end processing completed.");
    }

    private void Exit()
    {
        Console.WriteLine("Thank you for using MiniBank!");
        running = false;
    }
}
