namespace ReadingList.App;

public class CommandApp
{
    private readonly CommandDispatcher _dispatcher;

    public CommandApp(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Run()
    {
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null || string.IsNullOrEmpty(line))
            {
                continue;
            }
            if (line.Equals(Constants.ExistCommandKeyword, StringComparison.OrdinalIgnoreCase))
            {
                break;
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
}