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
public sealed partial class EnterGameValidationSystem(
    World world,
    SessionService sessions,
    ILogger<EnterGameValidationSystem> logger)
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<EnterGameIntent>]
    private void ProcessEnterGameIntent(in Entity e, ref EnterGameIntent intent)
    {
        var peerId = intent.PeerId;
        if (!sessions.TryGetAccount(peerId, out _))
        {
            logger.LogWarning("EnterGame denied: unauthenticated peer {Peer}", peerId);
            World.Destroy(e);
            return;
        }
        if (!sessions.TryGetSelectedCharacter(peerId, out var ch) || ch.CharacterId != intent.CharacterId)
        {
            logger.LogWarning("EnterGame denied: invalid character {Char} for peer {Peer}", intent.CharacterId, peerId);
            World.Destroy(e);
            return;
        }
        var req = new SpawnRequest {
            PeerId = peerId,
            CharacterId = ch.CharacterId,
            Name = ch.Name,
            Vocation = ch.Vocation,
            Gender = ch.Gender
        };
        World.Create(req);
        World.Destroy(e);
        
        logger.LogInformation("EnterGame approved: peer {Peer} character {Char}", peerId, intent.CharacterId);
    }
}
