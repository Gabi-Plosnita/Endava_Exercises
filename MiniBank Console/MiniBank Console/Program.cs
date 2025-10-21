using MiniBank_Console;
using MiniBank_Console.Services;
using MiniBank_Console.Services.Interfaces;
using System.Globalization;

Thread.CurrentThread.CurrentCulture = new CultureInfo("ro-RO");
Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");
IBankAccountService bankAccountService = new BankAccountService();
bankAccountService.SeedData();
var app = new MiniBankApp(bankAccountService);
app.Run();