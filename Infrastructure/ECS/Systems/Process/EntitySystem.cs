using System.Collections.Generic;
using Arch.Core;
using GameClient.Infrastructure.ECS.Components;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Network.Data.Join;

namespace GameClient.Infrastructure.ECS.Systems.Process;

/// <summary>
/// No CLIENTE, gere o ciclo de vida das entidades de jogadores no World do ECS.
/// Apenas lida com dados, não com nós da Godot.
/// </summary>
public class EntitySystem(ILogger<EntitySystem> logger, World world)
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

    public void CreatePlayerEntity(PlayerData data, bool isLocal = false)
    {
        if (_playersByNetId.ContainsKey(data.NetId))
        {
            logger.LogWarning("Tentativa de criar entidade para o jogador {NetId} que já existe.", data.NetId);
            return;
        }
        
        var entity = world.Create(
            new NetworkedTag { Id = data.NetId },
            new PlayerInfoComponent { Name = data.Name, Vocation = data.Vocation, Gender = data.Gender },
            new GridPositionComponent { Value = data.GridPosition },
            new SpeedComponent { Value = data.Speed },
            new DirectionComponent { Value = data.Direction }
        );
        
        if (isLocal)
            world.Add(entity, new PlayerControllerTag());
        else
            world.Add(entity, new RemoteProxyTag());

        _playersByNetId[data.NetId] = entity;
        logger.LogInformation("Entidade de dados criada para o jogador {Name} ({NetId}).", data.Name, data.NetId);
    }

    public void DisposePlayerEntity(int netId)
    {
        if (!_playersByNetId.Remove(netId, out var entity))
            return;
        
        if (world.IsAlive(entity))
            world.Destroy(entity);
        
        logger.LogInformation("Entidade de dados destruída para o jogador {NetId}.", netId);
    }
}