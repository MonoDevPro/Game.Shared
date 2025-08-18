using Arch.Core;
using Arch.System;
using Arch.Bus;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Events;
using Game.Server.Headless.Core.ECS.Game.Components.Tags;
using Shared.Network.Packets.Game.Player;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Replication;

/// <summary>
/// Emite PlayerData para o novo jogador e notifica os demais.
/// </summary>
public partial class SpawnReplicationSystem : BaseSystem<World, float>
{
    private readonly NetworkSender _sender;

    public SpawnReplicationSystem(World world, NetworkSender sender) : base(world)
    {
        _sender = sender;
        Hook();
    }

    [Event(order: 0)]
    public void OnPlayerSpawned(ref ReplicationSpawnEvent ev)
    {
        var evCopy = ev; // avoid using ref in lambda
        // Busca entidade e envia PlayerData para todos
        var q = new QueryDescription().WithAll<PlayerRegistryComponent>();
        World.Query(in q, (ref PlayerRegistryComponent registry) =>
        {
            if (!registry.PlayersByNetId.TryGetValue(evCopy.NetId, out var ent) || !World.IsAlive(ent))
                return;

            var info = World.Get<CharInfoComponent>(ent);
            var pos = World.Get<MapPositionComponent>(ent);
            var dir = World.Get<DirectionComponent>(ent);
            var spd = World.Get<SpeedComponent>(ent);
            var tag = World.Get<NetworkedTag>(ent);

            var dto = new PlayerData
            {
                NetId = tag.Id,
                Name = info.Name,
                Vocation = info.Vocation,
                Gender = info.Gender,
                Direction = dir.Value,
                Speed = spd.Value,
                GridPosition = pos.Value,
                Description = "Player spawned"
            };
            _sender.EnqueueReliableBroadcast(ref dto);
        });
    }

    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }
}
