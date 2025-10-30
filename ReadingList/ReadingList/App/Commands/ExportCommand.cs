
using ReadingList.Domain;

namespace ReadingList.App;

public class ExportCommand(IRepository<Book, int> _repository, IExportService<Book> _exportService) : ICommand
{
    public string Keyword => Resources.ExportCommandKeyword;

    public string Summary => Resources.ExportCommandSummary;

    public async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        

        
    }
}
