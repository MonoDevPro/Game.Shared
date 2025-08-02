using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Input;
using Game.Shared.Shared.Infrastructure.Spawners;

namespace Game.Shared.Server.Infrastructure.ECS.Systems;

public partial class ServerRenderSystem(World world, PlayerSpawner spawner) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;

    [Query]
    [All<NetworkedTag, GridPositionComponent>]
    private void Render(in Entity entity, in NetworkedTag netTag, in GridPositionComponent gridPos)
    {
        // Notifica todos os clientes sobre a posição atual do jogador.
        var packet = new StateResponse
        {
            NetId = netTag.Id,
            GridPosition = gridPos.Value
        };
        spawner.NetworkManager.Sender.Broadcast(ref packet);
    }
}