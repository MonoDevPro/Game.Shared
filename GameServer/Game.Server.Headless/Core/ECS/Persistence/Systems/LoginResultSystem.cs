using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components.Tags;
using GameServer.Infrastructure.EfCore.Worker;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Persistence.Systems;

public sealed partial class LoginResultSystem(World world, IBackgroundPersistence persistence, ILogger<LoginResultSystem> logger) : BaseSystem<World, float>(world)
{
    [Query]
    [All<CommandMetaComponent>]
    private void ProcessLoginResult(Entity entity, ref CommandMetaComponent meta)
    {
        var reader = persistence.LoginResults;
        // ler resultados sem bloquear
        while (reader.TryRead(out var res))
        {
            // localizar a entidade que tem CommandMetaComponent.CommandId == res.CommandId
            Entity? found = null;
            if (meta.CommandId == res.CommandId) 
                found = entity;

            if (found is null)
            {
                logger.LogWarning("Login result for command {CommandId} but no command entity found", res.CommandId);
                continue;
            }

            if (!res.Success || res.Character is null)
            {
                // enviar mensagem de erro ao cliente usando SenderPeer info guardado (você pode guardar SenderPeer no command entity)
                logger.LogInformation("Login failed for command {CommandId}: {Error}", res.CommandId, res.ErrorMessage);
                // opcional: adicionar component com erro para o sistema de rede enviar
                world.Destroy(found.Value);
                continue;
            }

            // sucesso: criar entidade jogador no mundo, mapear PlayerLoadModel -> components
            /*var playerEntity = world.CreateEntity();

            // exemplo de mapping mínimo
            world.Add(new NetworkedTag { Id = res.Player.PlayerId }, playerEntity);
            // mapear outros components: InventoryComponent, StatsComponent, PositionComponent, etc. com os dados de res.Player

            // remover entidade de comando original
            world.Destroy(found.Value);

            logger.LogInformation("Player {PlayerId} logged in (cmd {Cmd}).", res.Player.PlayerId, res.CommandId);*/
        }
    }
}