namespace ReadingList.App;

public class CommandApp
{
    private readonly CommandDispatcher _dispatcher;

    public CommandApp(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null || string.IsNullOrEmpty(line))
            {
                continue;
            }
            if (line.Equals(Resources.ExistCommandKeyword, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            if (line.Equals(Resources.HelpCommandKeyword, StringComparison.OrdinalIgnoreCase))
            {
                DisplayCommands();
                continue;
            }

            using var cts = new CancellationTokenSource();

            ConsoleCancelEventHandler? handler;
            handler = (_, e) =>
            {
                e.Cancel = true; 
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }
            };

            Console.CancelKeyPress += handler;
            try
            {
                await _dispatcher.DispatchLineAsync(line, cts.Token);
            }
            finally
            {
                Console.CancelKeyPress -= handler;
            }
        }
    }

    private void DisplayCommands()
    {
        Console.WriteLine("Available commands:");
        foreach (var cmd in _dispatcher.Commands)
        {
            Console.WriteLine($"- {cmd.Keyword}: {cmd.Summary}");
        }
    }
}