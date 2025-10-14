using MiniBank_Console;
using MiniBank_Console.Services;

var bankAccountService = new BankAccountService();
var app = new MiniBankApp(bankAccountService);
app.Run();