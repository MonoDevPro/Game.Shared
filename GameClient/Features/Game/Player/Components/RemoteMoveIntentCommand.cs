using Shared.Core.Common.Math;

namespace GameClient.Features.Game.Player.Components;

/// <summary>
/// Comando do servidor para o cliente informando a nova posição de uma entidade no grid.
/// </summary>
public struct RemoteMoveIntentCommand
{
    public GridVector Direction;
    public GridVector GridPosition;
}