using MiniBank_Console;
using MiniBank_Console.Services;

var app = new MiniBankApp(new BankAccountService());
app.Run();