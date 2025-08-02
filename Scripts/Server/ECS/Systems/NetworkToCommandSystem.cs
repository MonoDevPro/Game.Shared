using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using LiteNetLib;

namespace Game.Shared.Scripts.Server.ECS.Systems;

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
        _disposables.Add(_spawner.NetworkManager.Receiver.RegisterMessageHandler<InputRequest>(OnPlayerInputReceived));
    }
    
    public override void Update(in float t) => _spawner.NetworkManager.PollEvents();

    private void OnPlayerInputReceived(InputRequest packet, NetPeer peer)
    {
        if (!_spawner.TryGetPlayerByNetId(peer.Id, out var character))
            return;

        var entity = character.Entity;

        // IMPORTANTE: Previne que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
        // Se a entidade já tem uma intenção pendente ou já está se movendo, ignora o novo request.
        if (World.Has<MoveIntentCommand>(entity) || World.Has<IsMovingTag>(entity))
            return;
        
        // Adiciona o comando com a direção recebida e a tag para bloquear novos movimentos.
        World.Add(entity, new MoveIntentCommand { Direction = packet.Direction });
        World.Add<IsMovingTag>(entity);
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