using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Input;
using Game.Shared.Shared.Infrastructure.Spawners;
using LiteNetLib;

namespace Game.Shared.Server.Infrastructure.ECS.Systems;

/// <summary>
/// No servidor, converte o InputRequest (intenção de movimento) da rede 
/// em um MoveIntentCommand para ser processado pelo ProcessMovementSystem.
/// </summary>
public partial class NetworkToCommandSystem : BaseSystem<World, float>
{
    private readonly PlayerSpawner _spawner;
    private readonly List<IDisposable> _disposables = [];
    
    public NetworkToCommandSystem(World world, PlayerSpawner spawner) : base(world) 
    {
        _spawner = spawner;
        _disposables.Add(_spawner.NetworkManager.Receiver.RegisterMessageHandler<MovementRequest>(OnMovementRequestReceived));
    }
    
    public override void Update(in float t) => _spawner.NetworkManager.PollEvents();

    private void OnMovementRequestReceived(MovementRequest packet, NetPeer peer)
    {
        if (!_spawner.TryGetPlayerByNetId(peer.Id, out var character))
            return;
        
        var entity = character.Entity;

        // IMPORTANTE: Previne que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
        // Se a entidade já tem uma intenção pendente ou já está se movendo, ignora o novo request.
        if (World.Has<MoveIntentCommand>(entity))
            return;
        
        // Adiciona o comando com a direção recebida e a tag para bloquear novos movimentos.
        World.Add(entity, new MoveIntentCommand { Direction = packet.Direction });
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        
        base.Dispose();
    }
}