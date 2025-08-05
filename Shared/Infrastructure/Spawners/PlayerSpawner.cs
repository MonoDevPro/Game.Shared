using System.Collections.Generic;
using Game.Shared.Shared.Entities;
using Game.Shared.Shared.Infrastructure.ECS;
using Game.Shared.Shared.Infrastructure.Network;
using Game.Shared.Shared.Infrastructure.Network.Data.Join;
using Godot;
using LiteNetLib;

namespace Game.Shared.Shared.Infrastructure.Spawners;

public abstract partial class PlayerSpawner : Node2D
{
    [Export] private NodePath _networkPath;
    [Export] private NodePath _ecsPath;
    public NetworkManager NetworkManager { get; private set; }
    protected EcsRunner ECSRunner { get; private set; }
    
    protected readonly Dictionary<int, CharacterScript> _players = new();
    
    public override void _Ready()
    {
        NetworkManager = GetNode<NetworkManager>(_networkPath);
        ECSRunner = GetNode<EcsRunner>(_ecsPath);
        
        NetworkManager.PeerRepository.PeerDisconnected += OnPeerDisconnected;
        NetworkManager.PeerRepository.PeerConnected += OnPeerConnected;
    }

    // Client: Server disconnected, process the disconnection
    // Server: Client disconnected, process the player disconnection
    protected abstract void OnPeerDisconnected(NetPeer peer, string reason);
    
    // Client: Server connected, process the connection
    // Server: Client connected, process the player connection
    protected abstract void OnPeerConnected(NetPeer peer);
    
    public bool TryGetPlayerByNetId(int netId, out CharacterScript playerEntity)
    {
        if (_players.TryGetValue(netId, out playerEntity) 
            && ECSRunner.World.IsAlive(playerEntity.Entity))
            return true; // Player entity found
        
        GD.PrintErr($"[PlayerSpawner] No player entity found for ID: {netId}");
        return false; // Player entity not found
    }
    
    public bool TryGetPlayerByPeer(NetPeer peer, out CharacterScript playerEntity)
        => TryGetPlayerByNetId(peer.Id, out playerEntity);

    protected virtual CharacterScript CreatePlayer(ref PlayerData data)
    {
        // Create a new player entity using the ECS system
        var player = CharacterScript.Create(ECSRunner.World, data);
        
        // Add the player to the players dictionary
        if (AddPlayer(data.NetId, player))
            return player; // Return the created player entity

        GD.PrintErr("[PlayerSpawner] Failed to add player entity.");
        return null; // Failed to add player entity
    }
    
    protected bool AddPlayer(int netId, CharacterScript player)
    {
        if (_players.ContainsKey(netId))
        {
            GD.PrintErr($"[PlayerSpawner] Player with ID {netId} already exists.");
            return false; // Player already exists
        }

        if (player == null)
        {
            GD.PrintErr("[PlayerSpawner] Failed to create player entity.");
            return false; // Failed to create player entity
        }

        _players[netId] = player; // Add to the players dictionary
        AddChild(player); // Add the player entity to the scene tree
        
        GD.Print($"[PlayerSpawner] Added player entity for ID: {netId}");
        return true;
    }
    
    protected bool RemovePlayer(int id)
    {
        if (!_players.Remove(id, out var player))
        {
            GD.PrintErr($"[PlayerSpawner] No player entity found for ID: {id}");
            return false; // Player entity not found
        }
        player.QueueFree(); // Remove the player entity from the scene
        
        GD.Print($"[PlayerSpawner] Removed player entity for ID: {id}");
        return true;
    }

    public override void _ExitTree()
    {
        NetworkManager.PeerRepository.PeerDisconnected -= OnPeerDisconnected;
        NetworkManager.PeerRepository.PeerConnected -= OnPeerConnected;

        // Clear all players and entities on exit
        foreach (var player in _players.Values)
            player.QueueFree();

        _players.Clear();

        GD.Print("[PlayerSpawner] Exiting tree, all players removed.");
    }
}
