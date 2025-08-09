using System;

namespace GameClient.Infrastructure.Events;

/// <summary>
/// Um quadro de avisos central para eventos de jogo no cliente.
/// </summary>
public static class GameEvents
{
    public static event Action OnGameStarted;
    public static void RaiseGameStarted() => OnGameStarted?.Invoke();
    
    public static event Action OnGameEnded;
    public static void RaiseGameEnded() => OnGameEnded?.Invoke();
}