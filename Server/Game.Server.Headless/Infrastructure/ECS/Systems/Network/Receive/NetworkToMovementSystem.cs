using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.ECS.Components;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Systems;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Input;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network.Receive;

/// <summary>
/// Ouve os pacotes de rede de input de movimento e traduz-os
/// em componentes MoveIntentCommand para a entidade apropriada.
/// </summary>
public class NetworkToMovementSystem : BaseSystem<World, float>
{
    private readonly EntitySystem _entitySystem;
    private readonly ILogger<NetworkToMovementSystem> _logger;

    public NetworkToMovementSystem(World world, NetworkManager networkManager, EntitySystem entitySystem, ILogger<NetworkToMovementSystem> logger) : base(world)
    {
        _entitySystem = entitySystem;
        _logger = logger;
        networkManager.Receiver.RegisterMessageHandler<MovementRequest>(OnMovementRequestReceived);
    }

    private void OnMovementRequestReceived(MovementRequest packet, NetPeer peer)
    {
        if (!_entitySystem.PlayerExists(peer.Id))
            return;
        
        var entityId = _entitySystem.GetPlayerEntity(peer.Id);
        
        // Evita que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
        if (World.Has<MoveIntentCommand>(entityId) || World.Has<MovementStateComponent>(entityId))
            return; 
        
        // --- NOVA LÓGICA DE VALIDAÇÃO ---
        // Obtém o componente de estado de input do jogador
        ref var inputState = ref World.Get<ClientInputStateComponent>(entityId);
        
        // Valida o ID de sequência.
        // Se o ID do pacote for menor ou igual ao último que processamos, é um pacote antigo/duplicado.
        if (packet.SequenceId <= inputState.LastProcessedSequenceId)
        {
            _logger.LogWarning("Pacote de movimento antigo descartado para o Peer {PeerId}. Seq recebido: {ReceivedSeq}, Último processado: {LastSeq}", 
                peer.Id, packet.SequenceId, inputState.LastProcessedSequenceId);
            return; // Descarta o pacote
        }
        
        // Se o pacote é válido, atualizamos o último ID processado.
        inputState.LastProcessedSequenceId = packet.SequenceId;
        
        _logger.LogDebug("Comando de movimento válido recebido. Entidade: {EntityId}, Direção: {Direction}, Seq: {Sequence}", 
            entityId, packet.Direction, packet.SequenceId);
        
        // Adiciona o comando de intenção à entidade para ser processado pelo MovementValidationSystem.
        World.Add(entityId, new MoveIntentCommand { Direction = packet.Direction });
    }
}