using System.Collections.Generic;
using Arch.System;
using Game.Shared.Scripts.Client.ECS.Systems;
using Game.Shared.Scripts.Shared.ECS;
using Game.Shared.Scripts.Shared.ECS.Systems.Physics;
using Game.Shared.Scripts.Shared.ECS.Systems.Process;
using Game.Shared.Scripts.Shared.Network;
using Game.Shared.Scripts.Shared.Spawners;
using Godot;

namespace Game.Shared.Scripts.Client.ECS;

/// <summary>
/// Implementação específica do ECS para o cliente
/// Gerencia a integração entre ArchECS e LiteNetLib no lado cliente
/// </summary>
public partial class ClientECS : EcsRunner
{
    [Export] public NodePath NetworkManagerPath { get; set; }
    [Export] public NodePath PlayerSpawnerPath { get; set; }
    private NetworkManager _networkManager;
    private PlayerSpawner _playerSpawner;

    public override void _Ready()
    {
        _networkManager = GetNode<NetworkManager>(NetworkManagerPath);
        _playerSpawner = GetNode<PlayerSpawner>(PlayerSpawnerPath);

        if (_networkManager == null || _playerSpawner == null)
        {
            GD.PrintErr("[ClientECS] Dependências não encontradas!");
            return;
        }
        base._Ready();
        GD.Print("[ClientECS] Cliente ECS inicializado com sucesso");
    }


    protected override void OnCreateProcessSystems(List<ISystem<float>> systems)
    {
        // Sistemas visuais que rodam a cada frame
        systems.Add(new InterpolationSystem(World));
        systems.Add(new AnimationSystem(World)); // <--- ADICIONE ESTA LINHA
        GD.Print("[ClientECS] Sistemas de processo do cliente registrados");
    }

    protected override void OnCreatePhysicsSystems(List<ISystem<float>> systems)
    {
        // Ordem de execução da simulação de predição do cliente
        systems.Add(new NetworkToCommandSystem(World, _playerSpawner)); // Recebe estado
        systems.Add(new ReconciliationSystem(World));               // Corrige o estado
        systems.Add(new LocalInputProcessSystem(World));            // Lê input local
        systems.Add(new InputRequestSystem(World));                 // Inicia simulação local
        systems.Add(new NetworkSendSystem(World, _playerSpawner));  // Envia input ao servidor
        systems.Add(new InputApplySystem(World));
        systems.Add(new InputPhysicsSystem(World));
        systems.Add(new OutputPhysicsSystem(World));
        GD.Print("[ClientECS] Sistemas de física do cliente registrados");
    }
}
