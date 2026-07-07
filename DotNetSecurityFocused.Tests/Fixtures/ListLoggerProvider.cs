using Microsoft.Extensions.Logging;

namespace DotNetSecurityFocused.Tests.Fixtures;

public class ListLoggerProvider : ILoggerProvider
{
    public List<string> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new ListLogger(this);

    public void Dispose() { }

    private class ListLogger : ILogger
    {
        private readonly ListLoggerProvider _provider;
        public ListLogger(ListLoggerProvider provider) => _provider = provider;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_provider.Entries)
            {
                _provider.Entries.Add(formatter(state, exception));
            }
        }
    }
    private class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}