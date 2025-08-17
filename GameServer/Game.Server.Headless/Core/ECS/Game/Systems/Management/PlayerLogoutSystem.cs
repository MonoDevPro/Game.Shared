using Arch.Core;
using Arch.System;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Infrastructure.Repositories;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.Game.Player;
using Shared.Network.Repository;
using Shared.Network.Transport;

// Usings dos seus outros projetos...

namespace Game.Server.Headless.Core.ECS.Game.Systems.Management;

public class PlayerLogoutSystem : BaseSystem<World, float>, IDisposable
{
    private readonly ILogger<PlayerLogoutSystem> _logger;
    private readonly NetworkSender _sender;
    private readonly PeerRepository _peerRepository;
    private readonly SessionService _sessions;
    private readonly IDisposable _leftGameRequestSubscription;

    public PlayerLogoutSystem(
        World world,
        ILogger<PlayerLogoutSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        PeerRepository peerRepository,
        SessionService sessions) : base(world)
    {
        _logger = logger;
        _sender = sender;
        _peerRepository = peerRepository;
        _sessions = sessions;

        // Registra o evento de desconexão (causa 1)
        _peerRepository.PeerDisconnected += OnPeerDisconnected;

        // Registra o manipulador de mensagem (causa 2)
        _leftGameRequestSubscription = receiver.RegisterMessageHandler<LeftGameRequest>(OnLeftGameRequest);
    }

    /// <summary>
    /// Handler para quando a conexão do jogador cai (causa: desconexão).
    /// </summary>
    private void OnPeerDisconnected(NetPeer peer, string reason)
    {
        _logger.LogInformation("Peer {PeerId} desconectado. Motivo: {Reason}. Processando logout.", peer.Id, reason);
        // Chama o método central com a razão da desconexão.
        HandlePlayerLogout(peer, $"Disconnected ({reason})");
    }

    /// <summary>
    /// Handler para quando o jogador envia um pacote para sair (causa: requisição).
    /// </summary>
    private void OnLeftGameRequest(LeftGameRequest packet, NetPeer peer)
    {
        _logger.LogInformation("Peer {PeerId} solicitou saída do jogo. Processando logout.", peer.Id);
        // Chama o método central com a razão da requisição.
        HandlePlayerLogout(peer, "Requested by client");
    }

    /// <summary>
    /// MÉTODO CENTRAL: Contém toda a lógica de remoção de um jogador,
    /// independentemente da causa.
    /// </summary>
    private void HandlePlayerLogout(NetPeer peer, string reason)
    {
        // Tenta obter o personagem associado a este peer.
        if (!_sessions.TryGetSelectedCharacter(peer.Id, out var character))
        {
            _logger.LogWarning("Um peer ({PeerId}) sem personagem selecionado saiu. Razão: {Reason}. Apenas limpando a sessão.", peer.Id, reason);
            _sessions.Unbind(peer.Id); // Limpa a sessão se houver alguma.
            return;
        }

        _logger.LogInformation("Jogador '{CharacterName}' (Peer: {PeerId}) está saindo. Razão: {Reason}", character.Name, peer.Id, reason);

        // --- LÓGICA DE DESTRUIÇÃO AGORA ESTÁ AQUI ---
        Entity playerEntityToRemove = Entity.Null;
        int netId = peer.Id;

        // 1. Obtém o registro para encontrar a entidade e removê-la do dicionário.
        var registryQuery = new QueryDescription().WithAll<PlayerRegistryComponent>();
        World.Query(in registryQuery, (ref PlayerRegistryComponent registry) =>
        {
            if (registry.PlayersByNetId.Remove(netId, out var entity))
            {
                playerEntityToRemove = entity;
            }
        });

        // 2. Se a entidade foi encontrada, destrói ela.
        if (playerEntityToRemove != Entity.Null && World.IsAlive(playerEntityToRemove))
        {
            World.Destroy(playerEntityToRemove);
            _logger.LogInformation("Entidade destruída para o jogador {NetId}.", netId);
        }

        // 3. Desvincula a sessão do personagem.
        _sessions.Unbind(peer.Id);

        // 4. Notifica todos os outros jogadores que este personagem saiu.
        var leftResponse = new LeftResponse { NetId = peer.Id }; // Supondo que o NetId esteja no character
        _sender.EnqueueReliableBroadcastExcept(peer.Id, ref leftResponse); // Envia para todos, exceto para o peer que está saindo.
    }

    public override void Dispose()
    {
        // Limpa todas as inscrições de eventos para evitar vazamentos de memória.
        _peerRepository.PeerDisconnected -= OnPeerDisconnected;
        _leftGameRequestSubscription.Dispose();

        _logger.LogInformation("[CharacterLogoutSystem] Desativado e handlers removidos.");
        base.Dispose();
    }
}