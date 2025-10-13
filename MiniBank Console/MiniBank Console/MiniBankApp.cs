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
        // iterate bankAccountService.Accounts and print details
    }

    private void CreateAccount()
    {
        // input owner, choose type, enter initial balance
        // call _registry.CreateAccount(...)
    }

    private void Deposit()
    {
        // prompt for account id, amount, etc.
    }

    private void Withdraw()
    {
        // prompt for account id, amount, etc.
    }

    private void ViewStatement()
    {
        // prompt for account id and call PrintStatement()
    }

    private void RunMonthEnd()
    {
        // iterate all accounts, apply monthly interest, etc.
    }

    private void Exit()
    {
        Console.WriteLine("Thank you for using MiniBank!");
        running = false;
    }
}
