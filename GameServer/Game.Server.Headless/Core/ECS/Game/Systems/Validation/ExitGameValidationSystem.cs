using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Intents;
using Game.Server.Headless.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Validation;

/// <summary>
/// Valida intents de entrar no jogo com base na sessão e seleciona dados do personagem.
/// Produz SpawnRequest em caso de sucesso.
/// </summary>
public sealed partial class ExitGameValidationSystem : BaseSystem<World, float>
{
    private readonly SessionService _sessions;
    private readonly ILogger<ExitGameValidationSystem> _logger;

    public ExitGameValidationSystem(World world, SessionService sessions, ILogger<ExitGameValidationSystem> logger) : base(world)
    {
        _sessions = sessions;
        _logger = logger;
    }
    
    [Query]
    [All<ExitGameIntent>]
    private void ProcessExitGameIntent(in Entity entity, ref ExitGameIntent intent)
    {
        var peerId = intent.PeerId;
        if (!_sessions.TryGetAccount(peerId, out _))
        {
            _logger.LogWarning("ExitGame denied: unauthenticated peer {Peer}", peerId);
            World.Destroy(entity);
            return;
        }
        
        if (!_sessions.TryGetSelectedCharacter(peerId, out var ch) || ch.CharacterId != intent.CharacterId)
        {
            _logger.LogWarning("ExitGame denied: invalid character {Char} for peer {Peer}", intent.CharacterId, peerId);
            World.Destroy(entity);
            return;
        }
        _logger.LogInformation("Processing exit game for peer {Peer} character {Char}", peerId, ch.CharacterId);
        
        // Por exemplo, você pode remover o personagem selecionado da sessão:
        _sessions.ClearSelectedCharacter(peerId);
        
        World.Create(new DespawnRequest
        {
            PeerId = peerId,
            CharacterId = ch.CharacterId,
        });
        
        // Finalmente, destrói a entidade de intenção de saída do jogo.
        World.Destroy(entity);
    }
}
