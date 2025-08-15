using System.Collections.Concurrent;
using System.Threading.Channels;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Server.Headless.Core.ECS.Components;
using GameServer.Infrastructure.EfCore;
using GameServer.Infrastructure.EfCore.Worker;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Transport;
using Shared.Features.MainMenu.Account.AccountLogin;

namespace Game.Server.Headless.Core.ECS.Systems.Persistence;

public partial class LoginSystem : BaseSystem<World, float>
{
    private readonly ChannelWriter<LoginRequestMessage> _loginWriter;
    private readonly ChannelReader<LoginResultMessage> _loginResultReader;
    private readonly NetworkSender _sender;
    private readonly ILogger<LoginSystem> _logger;

    // buffer local para processar results no Update sem depender do Channel.ReadAllAsync
    private readonly ConcurrentQueue<LoginResultMessage> _pendingResults = new();

    public LoginSystem(World world, DatabaseWorker dbWorker, NetworkSender sender, ILogger<LoginSystem> logger) : base(world)
    {
        _loginWriter = dbWorker.LoginRequestWriter;
        _loginResultReader = dbWorker.LoginResultReader;
        _sender = sender;
        _logger = logger;
    }

    // Query: apenas enfileira o pedido (rápido)
    [Query]
    [All<LoginRequestComponent, SenderPeerComponent>]
    private void EnqueueLoginRequest(in Entity commandEntity, ref LoginRequestComponent request, ref SenderPeerComponent sender)
    {
        var msg = new LoginRequestMessage(sender.Value, request.Username, request.Password, commandEntity);
        // TryWrite para não bloquear o loop ECS. Se o channel estiver cheio, registramos e descartamos ou respondemos erro.
        if (!_loginWriter.TryWrite(msg))
        {
            _logger.LogWarning("Login queue full — dropping login request for {User}", request.Username);
            // Opcional: responder ao cliente que o servidor está ocupado
            var busyResponse = new AccountLoginResponse { Success = false, Message = "Server busy" };
            _sender.EnqueueReliableSend(sender.Value, ref busyResponse);

            // Também destruímos o comando para evitar leak se desejar
            if (World.IsAlive(commandEntity)) World.Destroy(commandEntity);
        }
    }

    public override void Update(in float dt)
    {
        EnqueueLoginRequestQuery(World);
            
        // Transferir resultados do Channel para fila local (não bloquear)
        while (_loginResultReader.TryRead(out var res))
            _pendingResults.Enqueue(res);

        // Processar resultados (aplicar no World e enviar rede)
        while (_pendingResults.TryDequeue(out var r))
        {
            try
            {
                if (r.Success)
                {
                    // Aqui crie a entidade do jogador / componentes necessários
                    // Exemplo simples: enviar resposta de sucesso
                    var response = new AccountLoginResponse { Success = true };
                    _sender.EnqueueReliableSend(r.SenderPeer, ref response);

                    _logger.LogInformation("Login success for peer {Peer}", r.SenderPeer);
                }
                else
                {
                    var response = new AccountLoginResponse { Success = false, Message = r.FailureReason };
                    _sender.EnqueueReliableSend(r.SenderPeer, ref response);
                    _logger.LogInformation("Login failed for peer {Peer}: {Reason}", r.SenderPeer, r.FailureReason);
                }

                // destruir o comando de login (se ainda existir)
                if (World.IsAlive(r.CommandEntity)) World.Destroy(r.CommandEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying login result for peer {Peer}", r.SenderPeer);
            }
        }
    }
}