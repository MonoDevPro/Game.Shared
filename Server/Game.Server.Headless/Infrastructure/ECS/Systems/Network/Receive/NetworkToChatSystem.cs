using Arch.Core;
using Arch.System;
using LiteNetLib;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Systems;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Chat;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network.Receive;

public partial class NetworkToChatSystem : BaseSystem<World, float>
{
    private readonly EntitySystem _entitySystem;
    private readonly NetworkManager _manager;

    public NetworkToChatSystem(World world, EntitySystem entitySystem, NetworkManager manager) : base(world)
    {
        _entitySystem = entitySystem;
        _manager = manager;
        
        var receiver = _manager.Receiver;
        receiver.RegisterMessageHandler<ChatMessageRequest>(OnChatMessageReceived);
    }

    private void OnChatMessageReceived(ChatMessageRequest packet, NetPeer peer)
    {
        // Validação básica
        if (string.IsNullOrWhiteSpace(packet.Message)) 
            return;
        
        if (!_entitySystem.PlayerExists(peer.Id))
            return;
        
        var entityId = _entitySystem.GetPlayerEntity(peer.Id);
        
        // Futuramente, você pegaria o nome de um 'PlayerInfoComponent'
        var senderName = World.Get<PlayerInfoComponent>(entityId).Name;
        var broadcastPacket = new ChatMessageBroadcast
        {
            SenderName = senderName,
            Message = packet.Message // No futuro, pode validar/filtrar a mensagem aqui
        };
        
        // Envia a mensagem para todos os jogadores
        _manager.Sender.EnqueueReliableBroadcast(ref broadcastPacket);
    }
}