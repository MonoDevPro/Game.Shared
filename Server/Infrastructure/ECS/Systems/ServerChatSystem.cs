using Arch.Core;
using Arch.System;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Chat;
using Game.Shared.Shared.Infrastructure.Spawners;
using LiteNetLib;

namespace Game.Shared.Server.Infrastructure.ECS.Systems;

public partial class ServerChatSystem : BaseSystem<World, float>
{
    private readonly PlayerSpawner _spawner;

    public ServerChatSystem(World world, PlayerSpawner spawner) : base(world)
    {
        _spawner = spawner;
        _spawner.NetworkManager.Receiver
            .RegisterMessageHandler<ChatMessageRequest>(OnChatMessageReceived);
    }

    private void OnChatMessageReceived(ChatMessageRequest packet, NetPeer peer)
    {
        // Validação básica
        if (string.IsNullOrWhiteSpace(packet.Message)) 
            return;
        
        // Tenta obter o personagem para pegar o nome
        if (!_spawner.TryGetPlayerByPeer(peer, out var character)) 
            return;
        
        // Futuramente, você pegaria o nome de um 'PlayerInfoComponent'
        var senderName = character.World.Get<PlayerInfoComponent>(character.Entity).Name;
        var broadcastPacket = new ChatMessageBroadcast
        {
            SenderName = senderName,
            Message = packet.Message // No futuro, pode validar/filtrar a mensagem aqui
        };
        
        // Envia a mensagem para todos os jogadores
        _spawner.NetworkManager.Sender.EnqueueReliableBroadcast(ref broadcastPacket);
    }
}