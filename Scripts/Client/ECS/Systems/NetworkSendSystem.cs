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

    public override void BeforeUpdate(in float t)
    {
        _writer.Reset();
        base.BeforeUpdate(in t);
    }
    
    public override void Update(in float delta)
    {
        _syncTimer += delta;
        if (_syncTimer < _syncRate)
        {
            return;
        }
        _syncTimer -= _syncRate;
        
        SendPlayerInputToServerQuery(World);
        
        base.Update(in delta);
    }

    // CORRIGIDO: A query agora lê o InputComponent, que é o estado persistente do input.
    [Query]
    [All<PlayerControllerTag>]
    private void SendPlayerInputToServer(in InputRequestCommand input)
    {
        var currentInput = input.Value;

        // LÓGICA CORRIGIDA:
        // A única razão para NÃO enviar um pacote é se estamos parados E
        // o último pacote que enviamos também foi de "parado".
        // Isso evita o envio de pacotes de (0,0) desnecessariamente.
        if (currentInput.IsZeroApprox() && _lastSentInput.IsZeroApprox())
            return;
        
        // Para qualquer outra situação (começar a mover, continuar movendo, parar de mover),
        // nós enviamos o pacote. O _syncTimer já controla a frequência.
        var packet = new InputRequest { Value = currentInput };
        _spawner.NetworkManager.Sender.SerializeData(_writer, ref packet);
        
        // Atualiza o último input enviado para a próxima verificação.
        _lastSentInput = currentInput;
    }

    public override void AfterUpdate(in float t)
    {
        if (_writer.Length > 0)
            _spawner.NetworkManager.Sender.SendToServer(_writer.AsReadOnlySpan(), DeliveryMethod.Unreliable);
        
        base.AfterUpdate(in t);
    }
}