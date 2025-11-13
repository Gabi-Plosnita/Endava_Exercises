using Cafe.Application;
using Cafe.ConsoleUI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.RegisterConsoleUI();
services.RegisterApplication();
services.RegisterInfrastructure();

var serviceProvider = services.BuildServiceProvider();

var menu = serviceProvider.GetRequiredService<Menu>();
menu.Run();