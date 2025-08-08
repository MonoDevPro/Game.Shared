using System.Collections.Generic;
using Game.Shared.Client.Infrastructure.Bootstrap;
using Game.Shared.Client.Presentation.Entities.Character;
using Godot;
using LiteNetLib;
using Shared.Infrastructure.ECS;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Join;

namespace Game.Shared.Client.Infrastructure.Spawners;

public abstract partial class PlayerSpawner : Node2D
{
    public NetworkManager NetworkManager => ClientBootstrap.Instance.ClientNetwork;
    protected EcsRunner ECSRunner => ClientBootstrap.Instance.ClientECS;
    
    protected readonly Dictionary<int, CharacterScript> Players = new();
    
    public override void _Ready()
    {
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
        if (Players.TryGetValue(netId, out playerEntity) 
            && ClientBootstrap.Instance.World.IsAlive(playerEntity.Entity))
            return true; // Player entity found
        
        GD.PrintErr($"[PlayerSpawner] No player entity found for ID: {netId}");
        return false; // Player entity not found
    }
    
    public bool TryGetPlayerByPeer(NetPeer peer, out CharacterScript playerEntity)
        => TryGetPlayerByNetId(peer.Id, out playerEntity);

    protected virtual CharacterScript CreatePlayer(ref PlayerData data)
    {
        // Create a new player entity using the ECS system
        var player = CharacterScript.Create(ClientBootstrap.Instance.World, data);
        
        // Add the player to the players dictionary
        if (AddPlayer(data.NetId, player))
            return player; // Return the created player entity

        GD.PrintErr("[PlayerSpawner] Failed to add player entity.");
        return null; // Failed to add player entity
    }
    
    protected bool AddPlayer(int netId, CharacterScript player)
    {
        if (Players.ContainsKey(netId))
        {
            GD.PrintErr($"[PlayerSpawner] Player with ID {netId} already exists.");
            return false; // Player already exists
        }

        if (player == null)
        {
            GD.PrintErr("[PlayerSpawner] Failed to create player entity.");
            return false; // Failed to create player entity
        }

        Players[netId] = player; // Add to the players dictionary
        AddChild(player); // Add the player entity to the scene tree
        
        GD.Print($"[PlayerSpawner] Added player entity for ID: {netId}");
        return true;
    }
    
    protected bool RemovePlayer(int id)
    {
        if (!Players.Remove(id, out var player))
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
        foreach (var player in Players.Values)
            player.QueueFree();

        Players.Clear();

        GD.Print("[PlayerSpawner] Exiting tree, all players removed.");
    }
}