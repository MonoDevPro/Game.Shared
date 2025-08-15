using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameClient.Core.Logger;

public class GodotLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, GodotLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new GodotLogger(name));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}