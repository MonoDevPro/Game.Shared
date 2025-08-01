using Game.Shared.Scripts.Shared.Network.Data.Join;
using Game.Shared.Scripts.Shared.Network.Data.Left;
using Game.Shared.Scripts.Shared.Network.Transport;
using Game.Shared.Scripts.Shared.Spawners;
using Godot;
using LiteNetLib;

namespace Game.Shared.Scripts.Server;

public partial class ServerPlayerSpawner : PlayerSpawner
{
    private NetworkReceiver Receiver => base.NetworkManager.Receiver;
    private NetworkSender Sender => base.NetworkManager.Sender;
    
    public override void _Ready()
    {
        base._Ready();
        
        Receiver.RegisterMessageHandler<JoinRequest>(RequestPlayerJoin);
        Receiver.RegisterMessageHandler<LeftRequest>(RequestPlayerLeft);

        GD.Print("[ServerPlayerSpawner] Ready - Player spawning logic can be initialized here.");
    }

    protected override void OnPeerDisconnected(NetPeer peer, string reason)
    {
        // Handle player disconnection
        GD.Print($"[PlayerSpawner] Player Disconnected with ID: {peer.Id} reason: {reason}");
        
        // Optionally, you can send a left request to the server
        var leftRequest = new LeftRequest();
        // Remove the player from the scene
        RequestPlayerLeft(leftRequest, peer);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        // Handle player connection
        GD.Print($"[PlayerSpawner] Player Connected with ID: {peer.Id}");
    }

    private void RequestPlayerJoin(JoinRequest packet, NetPeer peer)
    {
        // Load Player Data
        var playerData = new PlayerData
        {
            Name = packet.Name,
            NetId = peer.Id,
        };
        // Create a new player entity and add it to the scene
        var player = CreatePlayer(ref playerData);
        
        // Optionally, you can send a confirmation back to the client
        Sender.Broadcast(ref playerData);
    }
    
    private void RequestPlayerLeft(LeftRequest packet, NetPeer peer)
    {
        // Handle player left request
        if (RemovePlayer(peer.Id))
        {
            // Optionally, you can send a confirmation back to the client
            var leftResponse = new LeftResponse() { NetId = peer.Id};
            Sender.Broadcast(ref leftResponse);

            GD.Print($"[PlayerSpawner] Player Left with ID: {peer.Id}");
        }
    }
}
