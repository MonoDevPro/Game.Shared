using Arch.Core;
using Arch.System;
using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Tags;
using Game.Server.Headless.Core.ECS.Game.Components.Events;
using Arch.Bus;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Simulation;

/// <summary>
/// Consome SpawnRequest e cria entidades de jogador no mundo autoritativo.
/// </summary>
public partial class SpawnSystem(World world, ILogger<SpawnSystem> logger) : BaseSystem<World, float>(world)
{
    [Query]
    [All<SpawnRequest>]
    private void ProcessSpawnRequest(in Entity e, ref SpawnRequest req)
    {
        var peerId = req.PeerId;
        var name = req.Name;
        var vocation = req.Vocation;
        var gender = req.Gender;
        var ent = World.Create(
            new NetworkedTag { Id = peerId },
            new CharInfoComponent { Name = name, Vocation = vocation, Gender = gender },
            new MapPositionComponent { Value = new MapPosition(5, 5) },
            new SpeedComponent { Value = 1.0f },
            new DirectionComponent { Value = DirectionEnum.South },
            new ClientInputStateComponent { LastProcessedSequenceId = 0 }
        );
        // Atualizar registro global se existir
        var registryQ = new QueryDescription().WithAll<PlayerRegistryComponent>();
        bool updated = false;
        World.Query(in registryQ, (ref PlayerRegistryComponent reg) =>
        {
            reg.PlayersByNetId ??= new Dictionary<int, Entity>();
            reg.PlayersByNetId[peerId] = ent;
            updated = true;
        });
        if (!updated)
        {
            // Cria o singleton se não existir ainda
            World.Create(new PlayerRegistryComponent
            {
                PlayersByNetId = new Dictionary<int, Entity> { [peerId] = ent }
            });
        }

        // Dispara evento de spawn
        var ev = new ReplicationSpawnEvent { NetId = peerId };
        EventBus.Send(ref ev);
        World.Destroy(e);
        
        logger.LogInformation("Player {PeerId} spawned with entity {EntityId}.", peerId, ent.Id);
    }
}
