using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Tags;
using Game.Server.Headless.Core.ECS.Persistence.Components;
using GameServer.Infrastructure.EfCore.Worker;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Persistence;

public sealed partial class PlayerSaveSystem(
    World world, 
    IBackgroundPersistence persistence, 
    ILogger<PlayerSaveSystem> logger) : BaseSystem<World, float>(world)
{
    [Query]
    [All<CharInfoComponent, NetworkedTag, DirtyComponent>]
    [None<SavePendingComponent>]
    private void EnqueueSave(Entity entity, in NetworkedTag net)
    {
        // construir DTO para salvar (mapeie os components relevantes)
        var model = new CharacterSaveModel
        {
            CharacterId = net.Id,
            // outros campos...
        };

        var cmdId = Guid.NewGuid();
        var req = new SaveRequest(cmdId, net.Id, model, DateTime.UtcNow);

        // adicione SavePendingComponent para evitar duplicatas enquanto o save está em voo
        World.Add(entity, new SavePendingComponent { CommandId = cmdId });

        // enfileirar de forma não bloqueante
        var t = persistence.EnqueueSaveAsync(req).AsTask();
        var tag = net;
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
            {
                logger.LogWarning("Save queue full for player {PlayerId}", tag.Id);
                // opcional: remover SavePending para permitir re-tentativa
                // world.Remove<SavePendingComponent>(entity); // <-- não faça isto aqui (world não acessível na continuação)
            }
            else if (task.IsFaulted)
            {
                logger.LogError(task.Exception, "Erro ao enfileirar save para player {PlayerId}", tag.Id);
            }
        }, TaskScheduler.Default);
    }
    
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