using MiniBank_Console;
using MiniBank_Console.Services;
using MiniBank_Console.Services.Interfaces;

IBankAccountService bankAccountService = new BankAccountService();
var app = new MiniBankApp(bankAccountService);
app.Run();