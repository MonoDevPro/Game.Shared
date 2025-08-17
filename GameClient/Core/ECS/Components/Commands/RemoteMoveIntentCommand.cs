using Game.Core.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components.Commands;

/// <summary>
/// Comando do servidor para o cliente informando a nova posição de uma entidade no grid.
/// </summary>
public struct RemoteMoveIntentCommand
{
    public MapPosition Direction;
    public MapPosition GridPosition;
}