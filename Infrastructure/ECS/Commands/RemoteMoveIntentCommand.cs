using Shared.Infrastructure.Math;

namespace GameClient.Infrastructure.ECS.Commands;

/// <summary>
/// Comando do servidor para o cliente informando a nova posição de uma entidade no grid.
/// </summary>
public struct RemoteMoveIntentCommand
{
    public GridVector Direction;
    public GridVector GridPosition;
}