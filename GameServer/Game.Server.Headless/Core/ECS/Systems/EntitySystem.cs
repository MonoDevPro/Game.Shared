using Arch.Core;
using Arch.System;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Tags;
using Microsoft.Extensions.Logging;
using Shared.Game.Player;

namespace Game.Server.Headless.Core.ECS.Systems;

/// <summary>
/// No CLIENTE, gere o ciclo de vida das entidades de jogadores no World do ECS.
/// Apenas lida com dados, não com nós da Godot.
/// </summary>
public class EntitySystem(ILogger<EntitySystem> logger, World world) 
    : BaseSystem<World, float>(world)
{
    private readonly Dictionary<int, Entity> _playersByNetId = new();
    
    public bool PlayerExists(int netId)
    {
        return _playersByNetId.ContainsKey(netId);
    }
    
    public Entity GetPlayerEntity(int netId)
    {
        if (_playersByNetId.TryGetValue(netId, out var entity))
            return entity;

        logger.LogWarning("Tentativa de obter entidade para o jogador {NetId} que não existe.", netId);
        return Entity.Null;
    }
    
    public IReadOnlyDictionary<int, Entity> GetPlayerEntities()
    {
        // Retorna uma lista de todas as entidades de jogadores
        return _playersByNetId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public bool CreatePlayerEntity(PlayerData data, out Entity playerEntity)
    {
        if (_playersByNetId.ContainsKey(data.NetId))
        {
            logger.LogError($"[PlayerSpawner] Player with ID {data.NetId} already exists.");
            playerEntity = default; // Set to default if player already exists
            return false; // Player already exists
        }
        
        // Apenas cria a entidade no mundo ECS. NENHUM NÓ GODOT É CRIADO.
        playerEntity = World.Create(
            new NetworkedTag { Id = data.NetId },
            new CharInfoComponent()
            {
                Name = data.Name,
                Vocation = data.Vocation,
                Gender = data.Gender,
            },
            new MapPositionComponent() { Value = data.GridPosition },
            new SpeedComponent { Value = data.Speed },
            new DirectionComponent { Value = data.Direction }
        );
        
        _playersByNetId[data.NetId] = playerEntity; // Adiciona a entidade ao dicionário de jogadores
        logger.LogInformation("Entidade de dados criada para o jogador {NetId}.", data.NetId);
        return true;
    }

    public void DisposePlayerEntity(int netId)
    {
        if (!_playersByNetId.Remove(netId, out var entity))
            return;
        
        var playerInfo = World.Get<CharInfoComponent>(entity);

        if (World.Has<NetworkedTag>(entity))
            World.Remove<NetworkedTag>(entity);
        
        logger.LogInformation("Entidade de dados destruída para o jogador {NetId}.", netId);
    }
    
    public override void Dispose()
    {
        // Limpa o dicionário de jogadores
        _playersByNetId.Clear();
        
        logger.LogInformation("[EntitySystem] Stopped, all players removed.");
        base.Dispose();
    }
}