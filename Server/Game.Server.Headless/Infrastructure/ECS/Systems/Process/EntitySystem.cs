using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Network.Data.Join;
using Shared.Infrastructure.Network.Transport;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Process;

public partial class EntitySystem(
    ILogger<EntitySystem> logger,
    NetworkSender sender,
    World world) : BaseSystem<World, float>(world: world)
{
    public override void Initialize()
    {
        logger.LogInformation("[EntitySystem] Initialized, ready to manage player entities.");
        base.Initialize();
    }
    
    public override void Dispose()
    {
        CleanupPlayer();
        base.Dispose();
    }
    
    # region Player Management
    private readonly Dictionary<int, Entity> _playersByPeerId = new();
    
    private void CleanupPlayer()
    {
        // Clear all players and entities on stop
        foreach (var entity in _playersByPeerId.Values.ToList())
            World.Destroy(entity); // Remove the player entity from the scene
        
        _playersByPeerId.Clear();
        
        logger.LogInformation("[EntitySystem] Stopped, all players removed.");
    }
    
    public bool TryGetPlayerByNetId(int netId, out Entity playerEntity)
    {
        if (_playersByPeerId.TryGetValue(netId, out playerEntity) 
            && World.IsAlive(playerEntity))
            return true; // Player entity found
        
        logger.LogError($"[PlayerSpawner] No player entity found for ID: {netId}");
        return false; // Player entity not found
    }
    public bool TryGetPlayerByPeer(NetPeer peer, out Entity playerEntity)
        => TryGetPlayerByNetId(peer.Id, out playerEntity);
    
    public IReadOnlyDictionary<int, Entity> GetPlayers()
        => _playersByPeerId;
    
    public bool CreatePlayerEntity(ref PlayerData data, out Entity playerEntity)
    {
        if (_playersByPeerId.ContainsKey(data.NetId))
        {
            logger.LogError($"[PlayerSpawner] Player with ID {data.NetId} already exists.");
            playerEntity = default; // Set to default if player already exists
            return false; // Player already exists
        }
        
        // Apenas cria a entidade no mundo ECS. NENHUM NÓ GODOT É CRIADO.
        playerEntity = World.Create(
            new NetworkedTag { Id = data.NetId },
            new PlayerInfoComponent
            {
                Name = data.Name,
                Vocation = data.Vocation,
                Gender = data.Gender,
            },
            new GridPositionComponent { Value = data.GridPosition },
            new SpeedComponent { Value = data.Speed },
            new DirectionComponent { Value = data.Direction }
        );
        
        _playersByPeerId[data.NetId] = playerEntity; // Adiciona a entidade ao dicionário de jogadores
        return true;
    }
    
    public bool DisposePlayer(int netId)
    {
        if (!_playersByPeerId.Remove(netId, out var playerEntity))
        {
            logger.LogError($"[PlayerSpawner] No player entity found for ID: {netId}");
            return false; // Player entity not found
        }
        
        World.Destroy(playerEntity); // Remove the player entity from the world
        logger.LogInformation($"[PlayerSpawner] Removed player entity for ID: {netId}");
        return true;
    }
    
    # endregion
}