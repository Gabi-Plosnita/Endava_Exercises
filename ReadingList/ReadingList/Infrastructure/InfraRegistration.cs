using Microsoft.Extensions.DependencyInjection;
using ReadingList.Domain;

namespace ReadingList.Infrastructure;

public static class InfraRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        //services.AddScoped<IRepository<Book, int>, InMemoryRepository<Book, int>();
        services.AddScoped<IImportService, BookImportService>();
        return services;
    }
}
