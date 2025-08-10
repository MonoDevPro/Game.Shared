using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using GameClient.Infrastructure.ECS.Components;
using LiteNetLib;
using Shared.Infrastructure.ECS.Systems;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Join;
using Shared.Infrastructure.Network.Data.Left;

namespace GameClient.Infrastructure.ECS.Systems.Network.Receive;

public class NetworkToEntitySystem : BaseSystem<World, float>
{
    private readonly EntitySystem _entitySystem;
    private readonly List<IDisposable> _disposables = [];
    
    public NetworkToEntitySystem(World world, NetworkManager networkManager, EntitySystem entitySystem) : base(world)
    {
        _entitySystem = entitySystem;
        _disposables.AddRange([
            networkManager.Receiver.RegisterMessageHandler<PlayerData>(OnPlayerDataReceived),
            networkManager.Receiver.RegisterMessageHandler<LeftResponse>(OnLeftResponseReceived)
        ]);
    }

    private void OnPlayerDataReceived(PlayerData packet, NetPeer peer)
    {
        _entitySystem.CreatePlayerEntity(packet, out var entity);
        
        if (packet.NetId == peer.RemoteId)
            World.Add<PlayerControllerTag>(entity);
        else
            World.Add<RemoteProxyTag>(entity);
    }

    private void OnLeftResponseReceived(LeftResponse packet, NetPeer peer)
    {
        _entitySystem.DisposePlayerEntity(packet.NetId);
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        
        _disposables.Clear();
        base.Dispose();
    }
}