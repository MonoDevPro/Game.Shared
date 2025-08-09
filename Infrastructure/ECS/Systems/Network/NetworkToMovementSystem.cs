using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using GameClient.Infrastructure.ECS.Systems.Process;
using LiteNetLib;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Input;

namespace GameClient.Infrastructure.ECS.Systems.Network;

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
            networkManager.Receiver.RegisterMessageHandler<MovementUpdateResponse>(OnMovementUpdateReceived)
        ]);
    }
    
    /// <summary>
    /// Chamado quando uma atualização de estado (nova posição no grid) é recebida do servidor.
    /// </summary>
    private void OnMovementUpdateReceived(MovementUpdateResponse packet, NetPeer peer)
    {
        // Encontra a entidade do personagem correspondente ao NetId do pacote.
        if (!_entitySystem.PlayerExists(packet.NetId))
            return;

        var entity = _entitySystem.GetPlayerEntity(packet.NetId);

        // Adiciona um comando à entidade com a nova posição do grid.
        // O GridMovementSystem irá processar este comando para iniciar a interpolação visual.
        // A lógica é a mesma tanto para o jogador local quanto para os remotos.
        World.Add(entity, new MovementUpdateCommand
        {
            NetId = packet.NetId,
            NewGridPosition = packet.GridPosition
        });
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        
        base.Dispose();
    }
}