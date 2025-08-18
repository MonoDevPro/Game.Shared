using Arch.Core;
using Arch.System;
using Arch.Bus;
using Game.Server.Headless.Core.ECS.Game.Components.Events;
using Shared.Network.Packets.Game.Player;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Replication;

/// <summary>
/// Emite PlayerData para o novo jogador e notifica os demais.
/// </summary>
public partial class DespawnReplicationSystem : BaseSystem<World, float>
{
    private readonly NetworkSender _sender;

    public DespawnReplicationSystem(World world, NetworkSender sender) : base(world)
    {
        _sender = sender;
        Hook();
    }

    [Event(order: 0)]
    public void OnPlayerDespawned(ref ReplicationDespawnEvent ev)
    {
        var dto = new ExitGameResponse
        {
            NetId = ev.NetId,
        };
        _sender.EnqueueReliableBroadcast(ref dto);
    }

    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }
}
