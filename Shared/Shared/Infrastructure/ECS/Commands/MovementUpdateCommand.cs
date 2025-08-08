using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.ECS.Commands;

/// <summary>
/// Comando do servidor para o cliente informando a nova posição de uma entidade no grid.
/// </summary>
public struct MovementUpdateCommand
{
    public int NetId;
    public GridVector NewGridPosition;
}