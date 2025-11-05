using Microsoft.Extensions.DependencyInjection;
using ReadingList.App;
using ReadingList.Domain;
using System.IO.Abstractions;

namespace ReadingList.Infrastructure;

public static class InfraRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IImportService, BookImportService>();
        services.AddSingleton<IBookParser, CsvBookParser>();
        services.AddSingleton<IRepository<Book, int>>(sp => new InMemoryRepository<Book, int>(book => book.Id));

        services.AddSingleton<IExportService<Book>, ExportService<Book>>();
        services.AddSingleton<IExportStrategy<Book>, ExportCsvStrategy<Book>>();
        services.AddSingleton<IExportStrategy<Book>, ExportJsonStrategy<Book>>();

        return services;
    }
}
