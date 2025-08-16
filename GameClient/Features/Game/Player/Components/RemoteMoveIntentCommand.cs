using Game.Core.Entities.Common.ValueObjetcs;

namespace GameClient.Features.Game.Player.Components;

/// <summary>
/// Comando do servidor para o cliente informando a nova posição de uma entidade no grid.
/// </summary>
public struct RemoteMoveIntentCommand
{
    public MapPosition Direction;
    public MapPosition GridPosition;
}