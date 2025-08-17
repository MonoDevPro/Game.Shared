using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace Shared.Network;

/// <summary>
/// Uma ponte entre o sistema de log da LiteNetLib (INetLogger) e o
/// sistema de log padrão do .NET (ILogger).
/// </summary>
public class LiteNetLibLogger(ILogger<LiteNetLibLogger> logger) : INetLogger
{
    private readonly ILogger _logger = logger;

    // Usamos ILogger<LiteNetLibLogger> para que a categoria do log seja "LiteNetLibLogger"

    public void WriteNet(NetLogLevel level, string str, params object[] args)
    {
        var message = string.Format(str, args);
        
        switch (level)
        {
            case NetLogLevel.Info:
                _logger.LogInformation(message);
                break;
            case NetLogLevel.Warning:
                _logger.LogWarning(message);
                break;
            case NetLogLevel.Error:
                _logger.LogError(message);
                break;
            case NetLogLevel.Trace:
                _logger.LogTrace(message);
                break;
            default: // Inclui Debug0-4
                _logger.LogDebug(message);
                break;
        }
    }
}