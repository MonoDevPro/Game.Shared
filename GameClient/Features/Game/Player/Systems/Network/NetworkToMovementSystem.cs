using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using GameClient.Core.ECS.Systems;
using GameClient.Features.Game.Player.Components;
using LiteNetLib;
using Shared.Core.Network;
using Shared.Game.Player;

namespace GameClient.Features.Game.Player.Systems.Network;

// Renomeamos o sistema para refletir sua única responsabilidade
public partial class NetworkToMovementSystem : BaseSystem<World, float>
{
    private readonly List<IDisposable> _disposables = [];
    
    private readonly EntitySystem _entitySystem;

    public NetworkToMovementSystem(
        World world, 
        EntitySystem entitySystem, 
        NetworkManager networkManager) : base(world) 
    {
        _entitySystem = entitySystem;
        // Corrigido para ouvir a mensagem correta
        _disposables.AddRange(
        [
            networkManager.Receiver.RegisterMessageHandler<MovementStartResponse>(OnMovementUpdateReceived)
        ]);
    }
    
    /// <summary>
    /// Chamado quando uma atualização de estado (nova posição no grid) é recebida do servidor.
    /// </summary>
    private void OnMovementUpdateReceived(MovementStartResponse packet, NetPeer peer)
    {
        // Encontra a entidade do personagem correspondente ao NetId do pacote.
        if (!_entitySystem.PlayerExists(packet.NetId))
            return;

        var entity = _entitySystem.GetPlayerEntity(packet.NetId);

        // Adiciona um comando à entidade com a nova posição do grid.
        // O GridMovementSystem irá processar este comando para iniciar a interpolação visual.
        // A lógica é a mesma tanto para o jogador local quanto para os remotos.
        
        ref var remoteMoveCommand = ref World.AddOrGet<RemoteMoveIntentCommand>(entity);
        remoteMoveCommand.Direction = packet.TargetDirection;
        remoteMoveCommand.GridPosition = packet.CurrentPosition;
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        
        base.Dispose();
    }
}