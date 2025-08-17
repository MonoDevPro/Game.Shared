using Arch.Core;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.MainMenu.Character;
using Shared.Network.Packets.MainMenu.Character.CharacterList;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems.Characters;

public sealed class CharacterListSystem : BaseMainMenuSystem
{
    public CharacterListSystem(
        World world,
        ILogger<CharacterListSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions,
        IBackgroundPersistence persistence) : base(world, logger, receiver, sender, sessions, persistence)
    {
        RegisterHandler<CharacterListRequest>(OnCharacterListRequest);
    }

    public override void Update(in float t)
    {
        // Drain results without blocking the loop
        // 2) Character list
        var listReader = Persistence.CharacterListResults;
        while (listReader.TryRead(out var listRes))
        {
            var arr = listRes.Characters.Select(x => new CharacterData
            {
                CharacterId = x.CharacterId,
                Name = x.Name,
                Vocation = x.Vocation,
                Gender = x.Gender
            }).ToArray();
            var resp = new CharacterListResponse { Characters = arr };
            Sender.EnqueueReliableSend(listRes.SenderPeer, ref resp);
        }
    }
    
    private void OnCharacterListRequest(CharacterListRequest packet, NetPeer peer)
    {
        if (!Sessions.TryGetAccount(peer, out var accountId))
        {
            var empty = new CharacterListResponse { Characters = Array.Empty<CharacterData>() };
            Sender.EnqueueReliableSend(peer.Id, ref empty);
            return;
        }
        var cmdId = Guid.NewGuid();
        var req = new CharacterListRequestMsg(cmdId, accountId, peer.Id);
        var t = Persistence.EnqueueCharacterListAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                Logger.LogWarning("CharacterList queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                Logger.LogError(task.Exception, "Error enqueuing character list for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }
}