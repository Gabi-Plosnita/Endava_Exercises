using Microsoft.Extensions.DependencyInjection;

namespace ReadingList.App;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IBookService, BookService>();

        services.AddSingleton<CommandDispatcher>();
        services.AddSingleton<CommandApp>();

        services.AddSingleton<ICommand, ImportCommand>();
        services.AddSingleton<ICommand, ListAllCommand>();
        services.AddSingleton<ICommand, FilterFinishedCommand>();
        services.AddSingleton<ICommand, TopRatedCommand>();
        services.AddSingleton<ICommand, ByAuthorCommand>();
        services.AddSingleton<ICommand, StatsCommand>();
        services.AddSingleton<ICommand, MarkFinishedCommand>();
        services.AddSingleton<ICommand, RateCommand>();
        services.AddSingleton<ICommand, ExportCommand>();
        return services;
    }
}
