using Microsoft.Extensions.DependencyInjection;
using ReadingList.App;
using ReadingList.Infrastructure;
using Serilog;

// DI Container Setup //
var services = new ServiceCollection();

services.AddAppFileLogging(Constants.LoggingFilePath) 
        .AddApplication()           
        .AddInfrastructure();

var provider = services.BuildServiceProvider();

// Main Execution //
try
{
    var app = provider.GetRequiredService<CommandApp>();
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush(); 
    (provider as IDisposable)?.Dispose();
}