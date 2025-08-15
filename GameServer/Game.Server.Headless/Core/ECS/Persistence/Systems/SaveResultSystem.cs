using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components.Tags;
using GameServer.Infrastructure.EfCore.Worker;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Persistence.Systems;

public sealed partial class SaveResultSystem(World world, IBackgroundPersistence persistence, ILogger<SaveResultSystem> logger) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag, SavePendingComponent>]
    private void ProcessSaveResult(Entity entity, ref NetworkedTag net, in SavePendingComponent pending)
    {
        var reader = persistence.SaveResults;
        while (reader.TryRead(out var res))
        {
            // localizar entidade com NetworkedTag.Id == res.PlayerId
            Entity? found = null;
            var q = new QueryDescription().WithAll<NetworkedTag, SavePendingComponent>();
            World.Query(in q, (Entity e, ref NetworkedTag net, ref SavePendingComponent pending) =>
            {
                if (net.Id == res.CharacterId)
                {
                    // se multiple pending existirem, escolha a que bate no CommandId
                    if (pending.CommandId == res.CommandId) found = e;
                }
            });

            if (found is null)
            {
                logger.LogWarning("SaveResult for player {PlayerId} but no pending entity found", res.CharacterId);
                continue;
            }

            if (res.Success)
            {
                // remover flags Dirty e SavePending
                World.Remove<DirtyComponent>(found.Value);
                World.Remove<SavePendingComponent>(found.Value);
            }
            else
            {
                logger.LogWarning("Save failed for player {PlayerId}: {Error}. Marking for retry.", res.CharacterId, res.ErrorMessage);
                // política simples: remover SavePending para permitir re-tentativa
                World.Remove<SavePendingComponent>(found.Value);
                // opcional: incrementar contador de retries, e re-mark dirty (já estava)
            }
        }
    }
}