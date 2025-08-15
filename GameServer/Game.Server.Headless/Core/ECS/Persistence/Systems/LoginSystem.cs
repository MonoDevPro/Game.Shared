using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameServer.Infrastructure.EfCore.Worker;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Persistence.Systems;

public sealed partial class LoginSystem(World world, IBackgroundPersistence persistence, ILogger<LoginSystem> logger) : BaseSystem<World, float>(world)
{
    [Query]
    [All<LoginRequestComponent, SenderPeerComponent>]
    private void ProcessLoginRequest(Entity entity, ref LoginRequestComponent req, in SenderPeerComponent peer)
    {
        // gerar commandId e associar na entidade para rastrear resposta
        var cmdId = Guid.NewGuid();
        world.Add(entity, new CommandMetaComponent { CommandId = cmdId });
        
        // construir a mensagem de login
        var loginReq = new LoginRequest(cmdId, req.Username, req.PasswordHash, peer.PeerId);
        
        // enfileirar sem bloquear o sistema. Enfileiramento retorna ValueTask<bool>.
        // transformamos em Task e adicionamos continuation para logging
        var t = persistence.EnqueueLoginAsync(loginReq).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                logger.LogWarning("Login queue full: command {CommandId}", cmdId);
            else if (task.IsFaulted)
                logger.LogError(task.Exception, "Erro ao enfileirar login {CommandId}", cmdId);
        }, TaskScheduler.Default);

        // opicional: remover o component LoginRequestComponent para indicar que foi processado
        world.Remove<LoginRequestComponent>(entity);
    }
}