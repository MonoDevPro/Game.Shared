using Arch.Core;
using Arch.System;
using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Tags;
using Game.Server.Headless.Core.ECS.Game.Components.Events;
using Arch.Bus;
using Arch.System.SourceGenerator;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Simulation;

/// <summary>
/// Consome SpawnRequest e cria entidades de jogador no mundo autoritativo.
/// </summary>
public partial class DespawnSystem : BaseSystem<World, float>
{
    public DespawnSystem(World world) : base(world)
    {
        // Cria o singleton se não existir ainda
        World.Create(new PlayerRegistryComponent
        {
            PlayersByNetId = new Dictionary<int, Entity>()
        });
    }

    [Query]
    [All<DespawnRequest>]
    private void ProcessDespawnRequest(in Entity e, ref DespawnRequest req)
    {
        var peerId = req.PeerId;
        var characterId = req.CharacterId;
        
        // Verifica se o jogador existe
        var registryQ = new QueryDescription().WithAll<PlayerRegistryComponent>();
        World.Query(in registryQ, (ref PlayerRegistryComponent reg) =>
        {
            reg.PlayersByNetId.Remove(peerId, out var ent);
            if (ent != default && World.IsAlive(ent))
            {
                World.Destroy(ent);
            }
        });
        
        // Dispara evento de spawn
        var ev = new ReplicationDespawnEvent { NetId = peerId };
        EventBus.Send(ref ev);
        World.Destroy(e);
    }
}