using Arch.Core;
using Arch.System;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Intents;
using Game.Server.Headless.Core.ECS.Game.Components.States;
using Game.Server.Headless.Core.ECS.Game.Services;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Shared.Network.Packets.Game.Player;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Receive;

/// <summary>
/// Ouve os pacotes de rede de input de movimento e traduz-os
/// em componentes MoveIntent para a entidade apropriada.
/// </summary>
public class NetworkToMovementSystem : BaseSystem<World, float>
{
    private readonly PlayerLookupService _lookupService;
    private readonly ILogger<NetworkToMovementSystem> _logger;

    public NetworkToMovementSystem(World world, NetworkManager networkManager, PlayerLookupService lookupService, ILogger<NetworkToMovementSystem> logger) : base(world)
    {
        _lookupService = lookupService;
        _logger = logger;
        networkManager.Receiver.RegisterMessageHandler<MovementRequest>(OnMovementRequestReceived);
    }

    private void OnMovementRequestReceived(MovementRequest packet, NetPeer peer)
    {
        if (!_lookupService.TryGetPlayerEntity(peer.Id, out var entityId))
            return;
        
        // Evita que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
    if (World.Has<MoveIntent>(entityId) || World.Has<MovementProgressComponent>(entityId))
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
    World.Add(entityId, new MoveIntent { Direction = packet.Direction });
    }
}