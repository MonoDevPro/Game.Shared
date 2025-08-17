using Arch.Core;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.MainMenu.Character;
using Shared.Network.Packets.MainMenu.Character.CharacterCreation;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems.Characters;

public sealed class CharacterCreationSystem : BaseMainMenuSystem
{
    public CharacterCreationSystem(
        World world,
        ILogger<CharacterCreationSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions,
        IBackgroundPersistence persistence) : base(world, logger, receiver, sender, sessions, persistence)
    {
        RegisterHandler<CharacterCreationRequest>(OnCharacterCreationRequest);
    }

    public override void Update(in float t)
    {
        // 3) Character creation
        var creationReader = Persistence.CharacterCreationResults;
        while (creationReader.TryRead(out var cr))
        {
            if (!cr.Success || cr.Character is null)
            {
                var denied = new CharacterCreationResponse { Success = false, Message = cr.ErrorMessage ?? "Create failed" };
                Sender.EnqueueReliableSend(cr.SenderPeer, ref denied);
            }
            else
            {
                var dto = new CharacterData { CharacterId = cr.Character.CharacterId, Name = cr.Character.Name, Vocation = cr.Character.Vocation, Gender = cr.Character.Gender };
                var ok = new CharacterCreationResponse { Success = true, Message = "Character created", Character = dto };
                Sender.EnqueueReliableSend(cr.SenderPeer, ref ok);
            }
        }
    }
    
    private void OnCharacterCreationRequest(CharacterCreationRequest packet, NetPeer peer)
    {
        if (!Sessions.TryGetAccount(peer, out var accountId))
        {
            var denied = new CharacterCreationResponse { Success = false, Message = "Not logged" };
            Sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        var cmdId = Guid.NewGuid();
        // All name/format validations centralized in worker
        var req = new CharacterCreationRequestMsg(cmdId, accountId, packet.Name ?? string.Empty, packet.Vocation, packet.Gender, peer.Id);
        var t = Persistence.EnqueueCharacterCreationAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                Logger.LogWarning("CharacterCreation queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                Logger.LogError(task.Exception, "Error enqueuing character creation for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }
}