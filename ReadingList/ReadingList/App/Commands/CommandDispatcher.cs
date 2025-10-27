namespace ReadingList.App;

public class CommandDispatcher
{
    private readonly Dictionary<string, ICommand> _byKeyword;
    public IReadOnlyCollection<ICommand> Commands => _byKeyword.Values.Distinct().ToArray();

    public CommandDispatcher(IEnumerable<ICommand> commands)
    {
        _byKeyword = new(StringComparer.OrdinalIgnoreCase);

        foreach (var cmd in commands)
        {
            _byKeyword[cmd.Keyword] = cmd;
        }
    }

    public async Task DispatchLineAsync(string? line, CancellationToken ct)
    {
        var (keyword, args) = CommandLineParser.Parse(line);
        if (string.IsNullOrWhiteSpace(keyword))
            return;

        await DispatchAsync(keyword!, args, ct);
    }

    public async Task DispatchAsync(string keyword, string[] args, CancellationToken ct)
    {
        if (!_byKeyword.TryGetValue(keyword, out var cmd))
        {
            Console.Error.WriteLine($"Unknown command '{keyword}'. Type 'help' for available commands.");
            return;
        }

        await cmd.ExecuteAsync(args, ct);
        if(ct.IsCancellationRequested)
        {
            Console.WriteLine("Operation cancelled.");
        }
    }
}