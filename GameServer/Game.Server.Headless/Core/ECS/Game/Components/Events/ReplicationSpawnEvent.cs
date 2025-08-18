namespace Game.Server.Headless.Core.ECS.Game.Components.Events;

/// <summary>
/// Evento emitido quando um jogador entra no jogo e é spawnado.
/// </summary>
public struct ReplicationSpawnEvent
{
    public int NetId;
}
