using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Shared.Scripts.Client.ECS.Systems;

public partial class NetworkSendSystem : BaseSystem<World, float>
{
    private readonly PlayerSpawner _spawner;
    private readonly NetDataWriter _writer = new();
    private Vector2 _lastSentInput = Vector2.Zero;
    
    private readonly float _syncRate = 1f / 20f; // 20 vezes por segundo
    private float _syncTimer;

    public NetworkSendSystem(World world, PlayerSpawner spawner) : base(world)
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
        
        SendPlayerInputToServerQuery(World);
    }

    [Query]
    [All<PlayerControllerTag>]
    private void SendPlayerInputToServer(in NetworkedTag tag, in InputComponent input)
    {
        if (input.Value.Length() < 0.1f) // Se não houver input, não faz nada
            return;
        if (_lastSentInput != Vector2.Zero && input.Value.DistanceTo(_lastSentInput) < 0.01f)
            return; // Se o input não mudou significativamente, não envia
        
        // Verifica se o cliente está conectado
        if (!_spawner.TryGetPlayerByNetId(tag.Id, out var player))
            return;

        var packet1 = new InputRequest { Value = input.Value };
        _spawner.NetworkManager.Sender.SerializeData(_writer, ref packet1);
        
        _lastSentInput = input.Value; // Atualiza o último input enviado
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void AfterUpdate(in float t)
    {
        // Envia os dados serializados para todos os clientes conectados
        if (_writer.Length > 0)
            _spawner.NetworkManager.Sender.SendToServer(_writer.AsReadOnlySpan(), DeliveryMethod.Unreliable);
        
        base.AfterUpdate(in t);
    }
}
