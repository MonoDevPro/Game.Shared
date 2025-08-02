using System.Collections.Generic;
using Arch.System;
using Game.Shared.Server.Infrastructure.ECS.Systems;
using Game.Shared.Shared.Infrastructure.ECS;
using Game.Shared.Shared.Infrastructure.Network;
using Game.Shared.Shared.Infrastructure.Spawners;
using Godot;

namespace Game.Shared.Server.Infrastructure.ECS;

/// <summary>
/// Implementação específica do ECS para o servidor
/// Gerencia a integração entre ArchECS e LiteNetLib no lado servidor
/// </summary>
public partial class ServerECS : EcsRunner
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
            GD.PrintErr("[ServerECS] Dependências não encontradas!");
            return;
        }
        
        base._Ready();
        GD.Print("[ServerECS] Servidor ECS inicializado com sucesso");
    }

    protected override void OnCreateProcessSystems(List<ISystem<float>> systems)
    {
        // Adicione seus sistemas de processo do servidor aqui
        // Ex: systems.Add(new ServerInputProcessSystem(World, _networkManager));
        // Ex: systems.Add(new GameLogicSystem(World));
        GD.Print("[ServerECS] Sistemas de processo do servidor registrados");
    }

    protected override void OnCreatePhysicsSystems(List<ISystem<float>> systems)
    {
        // Ordem de execução da simulação do servidor
        systems.Add(new NetworkToCommandSystem(World, _playerSpawner));
        systems.Add(new ProcessMovementSystem(World, _playerSpawner));
        GD.Print("[ServerECS] Sistemas de física do servidor registrados");
    }
}
