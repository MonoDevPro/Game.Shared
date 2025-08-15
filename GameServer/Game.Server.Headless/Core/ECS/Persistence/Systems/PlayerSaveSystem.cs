using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components.Tags;
using GameServer.Infrastructure.EfCore.Worker;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Persistence.Systems;

public sealed partial class PlayerSaveSystem(
    World world, 
    IBackgroundPersistence persistence, 
    ILogger<PlayerSaveSystem> logger) : BaseSystem<World, float>(world)
{

    [Query]
    [All<NetworkedTag, DirtyComponent>]
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
}