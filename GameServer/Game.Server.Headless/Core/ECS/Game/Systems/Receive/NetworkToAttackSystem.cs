using Arch.Core;
using Arch.System;
using Game.Server.Headless.Core.ECS.Game.Components.Intents;
using Game.Server.Headless.Core.ECS.Game.Services;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Shared.Network.Packets.Game.Player;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Receive;

public sealed class NetworkToAttackSystem : BaseSystem<World, float>
{
    private readonly ILogger<NetworkToAttackSystem> _logger;
    private readonly PlayerLookupService _lookupService;
    private readonly List<IDisposable> _disposables = [];

    public NetworkToAttackSystem(ILogger<NetworkToAttackSystem> logger, World world, NetworkManager netManager, PlayerLookupService lookupService) : base(world)
    {
        _logger = logger;
        _lookupService = lookupService;
        _disposables.AddRange([
            netManager.Receiver.RegisterMessageHandler<AttackRequest>(OnAttackReceived),
        ]);
    }

    // Exemplo de método para lidar com ataques recebidos
    private void OnAttackReceived(AttackRequest packet, NetPeer peer)
    {
        if (!_lookupService.TryGetPlayerEntity(peer.Id, out var playerEntity))
        {
            _logger.LogWarning("Jogador com ID {PlayerId} não encontrado ao processar ataque.", peer.Id);
            return;
        }

        // Lógica para processar o ataque recebido
        if (packet.Direction == default)
        {
            _logger.LogWarning("Direção inválida recebida no ataque de {PlayerId}.", peer.Id);
            return;
        }

        if (!World.Has<AttackIntent>(playerEntity))
        {
            World.Add<AttackIntent>(playerEntity);
            _logger.LogInformation("Ataque recebido do jogador {PlayerId} na direção {Direction}.", peer.Id, packet.Direction);
        }
        else
        {
            _logger.LogWarning("Jogador {PlayerId} já tem um ataque pendente.", peer.Id);
        }
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
        base.Dispose();
    }
}