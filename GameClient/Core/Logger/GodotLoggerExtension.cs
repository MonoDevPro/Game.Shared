using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameClient.Core.Logger;

public static class GodotLoggerExtensions
{
    public static ILoggingBuilder AddGodotLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, GodotLoggerProvider>();
        return builder;
    }
}