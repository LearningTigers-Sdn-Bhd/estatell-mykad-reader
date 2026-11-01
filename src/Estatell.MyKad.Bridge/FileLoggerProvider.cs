using System.Text;

namespace Estatell.MyKad.Bridge;

/// <summary>
/// The file log: one line per entry, <c>time level category message</c>. It writes what the code logs and nothing else,
/// and the code never logs card data. An exception is written as its type and message, without the stack.
/// See docs/02-bridge/02-security.md#logs.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly RotatingFileWriter? _writer;
    private readonly TimeProvider _clock;

    public FileLoggerProvider(LogFileOptions options, TimeProvider clock)
    {
        _writer = options.Path is null ? null : new RotatingFileWriter(options.Path);
        _clock = clock;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_writer, _clock, categoryName[(categoryName.LastIndexOf('.') + 1)..]);

    public void Dispose()
    {
    }

    private sealed class FileLogger(RotatingFileWriter? writer, TimeProvider clock, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => writer is not null && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (writer is null || !IsEnabled(logLevel))
            {
                return;
            }

            var line = new StringBuilder()
                .Append(clock.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                .Append(' ').Append(Short(logLevel))
                .Append(' ').Append(category)
                .Append(' ').Append(formatter(state, null));
            if (exception is not null)
            {
                line.Append(" | ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);
            }

            writer.WriteLine(line.Replace('\r', ' ').Replace('\n', ' ').ToString());
        }

        private static string Short(LogLevel level) => level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            _ => "CRIT ",
        };
    }
}
