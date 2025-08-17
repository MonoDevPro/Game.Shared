using Arch.Core;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.MainMenu.Character;
using Shared.Network.Packets.MainMenu.Character.CharacterSelection;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems.Characters;

public sealed class CharacterSelectionSystem : BaseMainMenuSystem
{
    public CharacterSelectionSystem(
        World world,
        ILogger<CharacterSelectionSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions,
        IBackgroundPersistence persistence) : base(world, logger, receiver, sender, sessions, persistence)
    {
        RegisterHandler<CharacterSelectionRequest>(OnCharacterSelectionRequest);
    }

    public override void Update(in float t)
    {
        // Drain results without blocking the loop
        // 4) Character selection
        var selReader = Persistence.CharacterSelectionResults;
        while (selReader.TryRead(out var sr))
        {
            if (!sr.Success || sr.Character is null)
            {
                var denied = new CharacterSelectionResponse { Success = false, Message = sr.ErrorMessage ?? "Character not found" };
                Sender.EnqueueReliableSend(sr.SenderPeer, ref denied);
            }
            else
            {
                var dto = new CharacterData { CharacterId = sr.Character.CharacterId, Name = sr.Character.Name, Vocation = sr.Character.Vocation, Gender = sr.Character.Gender };
                // Persist selection into session for later EnterGame request
                Sessions.SetSelectedCharacter(sr.SenderPeer, dto);
                var ok = new CharacterSelectionResponse { Success = true, Message = "Character selected", Character = dto };
                Sender.EnqueueReliableSend(sr.SenderPeer, ref ok);
            }
        }
    }

    private void OnCharacterSelectionRequest(CharacterSelectionRequest packet, NetPeer peer)
    {
        if (!Sessions.TryGetAccount(peer, out var accountId))
        {
            var denied = new CharacterSelectionResponse { Success = false, Message = "Not logged" };
            Sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        var cmdId = Guid.NewGuid();
        var req = new CharacterSelectionRequestMsg(cmdId, accountId, packet.CharacterId, peer.Id);
        var t = Persistence.EnqueueCharacterSelectionAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                Logger.LogWarning("CharacterSelection queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                Logger.LogError(task.Exception, "Error enqueuing character selection for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }
}