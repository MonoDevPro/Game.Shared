using Arch.Core;
using Arch.System;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Services;
using LiteNetLib;
using Shared.Network;
using Shared.Network.Packets.Game.Chat;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Receive;

public partial class NetworkToChatSystem : BaseSystem<World, float>
{
    private readonly PlayerLookupService _lookupService;
    private readonly NetworkManager _manager;

    public NetworkToChatSystem(World world, PlayerLookupService lookupService, NetworkManager manager) : base(world)
    {
        _lookupService = lookupService;
        _manager = manager;
        
        var receiver = _manager.Receiver;
        receiver.RegisterMessageHandler<ChatMessageRequest>(OnChatMessageReceived);
    }

    private void OnChatMessageReceived(ChatMessageRequest packet, NetPeer peer)
    {
        // Validação básica
        if (string.IsNullOrWhiteSpace(packet.Message)) 
            return;

        if (!_lookupService.TryGetPlayerEntity(peer.Id, out var playerEntity))
            return;
        
        var senderName = World.Get<CharInfoComponent>(playerEntity).Name;
        var broadcastPacket = new ChatMessageBroadcast
        {
            SenderName = senderName,
            Message = packet.Message // No futuro, pode validar/filtrar a mensagem aqui
        };
        
        // Envia a mensagem para todos os jogadores
        _manager.Sender.EnqueueReliableBroadcast(ref broadcastPacket);
    }
}