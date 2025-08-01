using Arch.Core;
using Arch.System;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using LiteNetLib;

namespace Game.Shared.Scripts.Server.ECS.Systems;

// Renomeamos o sistema para refletir sua única responsabilidade
public partial class NetworkToCommandSystem : BaseSystem<World, float>
{
    private readonly PlayerSpawner _spawner;
    private readonly List<IDisposable> _disposables = [];
    
    public NetworkToCommandSystem(World world, PlayerSpawner spawner) : base(world) 
    {
        _spawner = spawner;
        
        _disposables.Add(_spawner.NetworkManager.Receiver.RegisterMessageHandler<InputRequest>(OnPlayerInputReceived));
    }
    
    public override void Update(in float t) => _spawner.NetworkManager.PollEvents();

    private void OnPlayerInputReceived(InputRequest packet, NetPeer peer)
    {
        if (!_spawner.TryGetPlayerByNetId(peer.Id, out var entity))
            return;
        
        World.Add(entity.Entity, new InputRequestCommand { Value = packet.Value });
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        
        base.Dispose();
    }
}
