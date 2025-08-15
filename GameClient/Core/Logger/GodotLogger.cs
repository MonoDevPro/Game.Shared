using System;
using Godot;
using Microsoft.Extensions.Logging;

namespace GameClient.Core.Logger;

public class GodotLogger(string categoryName) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Formata a mensagem para incluir nível e categoria, o que é ótimo para depuração.
        var message = $"[{logLevel.ToString().ToUpper()}] [{categoryName}] {formatter(state, exception)}";
        if (exception != null)
        {
            message += $"\n{exception}";
        }

        // Para logs de aviso e erro, usamos GD.PrintErr para que apareçam em vermelho no editor.
        if (logLevel >= LogLevel.Warning)
        {
            GD.PrintErr(message);
        }
        else
        {
            GD.Print(message);
        }
    }
}