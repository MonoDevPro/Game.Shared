using Game.Shared.Client.Presentation.Entities.Character.Sprites;
using Game.Shared.Shared.Enums;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Join;
using Game.Shared.Shared.Infrastructure.Network.Data.Left;
using Game.Shared.Shared.Infrastructure.Network.Transport;
using Game.Shared.Shared.Infrastructure.Spawners;
using Godot;
using LiteNetLib;

namespace Game.Shared.Client.Infrastructure.Spawners;

public partial class ClientPlayerSpawner : PlayerSpawner
{
    // Adicione uma referência à sua UI de criação de personagem
    [Export] private Window _createCharacterWindow;
    
    private NetworkReceiver Receiver => base.NetworkManager.Receiver;
    private NetworkSender Sender => base.NetworkManager.Sender;
    
    
    public override void _Ready()
    {
        base._Ready();
        
        Receiver.RegisterMessageHandler<PlayerData>(PlayerDataReceived);
        Receiver.RegisterMessageHandler<LeftResponse>(PlayerLeftReceived);
        
        GD.Print("[ClientPlayerSpawner] Ready - Player spawning logic can be initialized here.");
    }
    
    protected override void OnPeerDisconnected(NetPeer peer, string reason)
    {
        // Change Scenes... Client Disconnected
        GD.Print($"[PlayerSpawner] Player Disconnected with ID: {peer.Id} reason: {reason}");
        GetTree().Quit(1);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        // Connected to the server, you can handle player connection logic here
        GD.Print($"[PlayerSpawner] Player Connected with ID: {peer.Id}");
        
        // Mostra a janela para o jogador criar seu personagem.
        _createCharacterWindow?.Show();
    }

    private void PlayerDataReceived(PlayerData packet, NetPeer peer)
    {
        // Create a new player entity and add it to the scene
        var player = CreatePlayer(ref packet);

        var characterSprite = CharacterSprite.Create(packet.Vocation, packet.Gender);
        player.AddChild(characterSprite);
        
        // Adiciona o componente de sprite de animação ao cliente
        player.World.Add(player.Entity, new SpriteRefComponent { Value = characterSprite});
        
        if (packet.NetId == peer.RemoteId)
            player.World.Add(player.Entity, new PlayerControllerTag());
        else
            player.World.Add(player.Entity, new RemoteProxyTag());
    }
    
    private void PlayerLeftReceived(LeftResponse packet, NetPeer peer)
    {
        // Handle player left request
        if (RemovePlayer(packet.NetId))
            GD.Print($"[PlayerSpawner] Player Left with ID: {peer.Id}");
    }
}
