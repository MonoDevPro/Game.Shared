using System.Linq;
using Game.Shared.Shared.Enums;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Join;
using Game.Shared.Shared.Infrastructure.Network.Data.Left;
using Game.Shared.Shared.Infrastructure.Network.Transport;
using Game.Shared.Shared.Infrastructure.Spawners;
using Godot;
using LiteNetLib;

namespace Game.Shared.Server.Infrastructure.Spawners;

public partial class ServerPlayerSpawner : PlayerSpawner
{
    private NetworkReceiver Receiver => base.NetworkManager.Receiver;
    private NetworkSender Sender => base.NetworkManager.Sender;
        
    public override void _Ready()
    {
        base._Ready();
        Receiver.RegisterMessageHandler<JoinRequest>(RequestPlayerJoin);
        Receiver.RegisterMessageHandler<LeftRequest>(RequestPlayerLeft);
        GD.Print("[ServerPlayerSpawner] Ready.");
    }

    protected override void OnPeerDisconnected(NetPeer peer, string reason)
    {
        GD.Print($"[PlayerSpawner] Player Disconnected with ID: {peer.Id} reason: {reason}");
        RequestPlayerLeft(new LeftRequest(), peer);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        GD.Print($"[PlayerSpawner] Player Connected with ID: {peer.Id}");
    }

    private void RequestPlayerJoin(JoinRequest packet, NetPeer peer)
    {
        GD.Print($"Player '{packet.Name}' (ID: {peer.Id}) está tentando entrar.");
            
        // 1. Criar os dados e o personagem para o NOVO jogador.
        var newPlayerData = new PlayerData
        {
            Name = packet.Name,
            NetId = peer.Id,
            Vocation = packet.Vocation, // Pode ser dinâmico no futuro
            Gender = packet.Gender,      // Pode ser dinâmico no futuro
            GridPosition = new Vector2I(5, 5) // Posição inicial
        };
        var newPlayerCharacter = CreatePlayer(ref newPlayerData);

        // 2. Notificar TODOS os jogadores (incluindo o novo) sobre o novo jogador.
        Sender.EnqueueReliableBroadcast(ref newPlayerData);

        // 3. Notificar APENAS o novo jogador sobre todos os outros que JÁ estavam na sala.
        //    *** ESTA É A PARTE CORRIGIDA ***
        var allOtherPlayersData = _players.Values
            .Where(p => p.Entity != newPlayerCharacter.Entity) // Exclui o jogador que acabou de entrar
            .Select(p => {
                // Para cada jogador existente, buscamos os componentes ATUAIS da entidade ECS
                var world = p.World;
                var entity = p.Entity;
            
                // Retorna um PlayerData preenchido com o estado mais recente
                var playerInfo = world.Get<PlayerInfoComponent>(entity);
                return new PlayerData
                {
                    NetId = world.Get<NetworkedTag>(entity).Id,
                    GridPosition = world.Get<GridPositionComponent>(entity).Value,
                    Direction = world.Get<DirectionComponent>(entity).Value,
                    Speed = world.Get<SpeedComponent>(entity).Value,
                    // Preencha outros dados que sejam relevantes
                    Name = playerInfo.Name, // Você precisaria de um componente para o nome
                    Vocation = playerInfo.Vocation, // E para a vocação, etc.
                    Gender = playerInfo.Gender
                };
            })
            .ToArray();

        if (allOtherPlayersData.Length > 0)
            Sender.SendArrayNow(allOtherPlayersData, peer.Id, DeliveryMethod.ReliableOrdered);
    }
        
    private void RequestPlayerLeft(LeftRequest packet, NetPeer peer)
    {
        if (RemovePlayer(peer.Id))
        {
            var leftResponse = new LeftResponse() { NetId = peer.Id };
            Sender.EnqueueReliableBroadcast(ref leftResponse);
            GD.Print($"[PlayerSpawner] Player Left with ID: {peer.Id}");
        }
    }
}