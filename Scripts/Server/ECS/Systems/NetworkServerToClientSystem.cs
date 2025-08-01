using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Shared.Scripts.Server.ECS.Systems;

public partial class NetworkServerToClientSystem : BaseSystem<World, float>
{
    private readonly PlayerSpawner _spawner;
    private readonly NetDataWriter _writer = new();
    private readonly float _syncRate = 1f / 20f; // 20 vezes por segundo
    private float _syncTimer;

    public NetworkServerToClientSystem(World world, PlayerSpawner spawner) : base(world)
    {
        _spawner = spawner;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void BeforeUpdate(in float t)
    {
        _writer.Reset();
        
        base.BeforeUpdate(in t);
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Update(in float delta)
    {
        _syncTimer += delta;
        if (_syncTimer < _syncRate)
        {
            return;
        }
        _syncTimer -= _syncRate;
        
        SyncPlayerPositionToClientQuery(World, in delta);
    }

    [Query]
    [All<NetworkedTag>]
    private void SyncPlayerPositionToClient([Data] in float delta, in NetworkedTag tag, SceneBodyRefComponent body)
    {
        // Verifica se o cliente está conectado
        if (!_spawner.TryGetPlayerByNetId(tag.Id, out var player))
            return;

        var packet1 = new StateResponse { NetId = tag.Id, Position = body.Value.GlobalPosition, Velocity = body.Value.Velocity };
        _spawner.NetworkManager.Sender.SerializeData(_writer, ref packet1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void AfterUpdate(in float t)
    {
        // Envia os dados serializados para todos os clientes conectados
        if (_writer.Length > 0)
            _spawner.NetworkManager.Sender.BroadcastData(_writer.AsReadOnlySpan(), DeliveryMethod.Unreliable);
        
        base.AfterUpdate(in t);
    }
}
