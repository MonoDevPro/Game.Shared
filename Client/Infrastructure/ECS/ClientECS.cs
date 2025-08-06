using System.Collections.Generic;
using Arch.System;
using Game.Shared.Client.Infrastructure.ECS.Systems;
using Game.Shared.Client.Presentation.UI.Chat;
using Game.Shared.Shared.Infrastructure.ECS;
using Game.Shared.Shared.Infrastructure.ECS.Systems;
using Game.Shared.Shared.Infrastructure.Network;
using Game.Shared.Shared.Infrastructure.Spawners;
using Godot;

namespace Game.Shared.Client.Infrastructure.ECS;

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
        // Sistemas visuais
        systems.Add(new AnimationSystem(World));
        
        var chatUI = GetNode<ChatUI>("/root/ClientBootstrap/GameUI/ChatUI"); // Exemplo de caminho
        systems.Add(new ClientChatSystem(World, _networkManager, chatUI));
        GD.Print("[ClientECS] Sistemas de processo do cliente registrados");
    }
    
    protected override void OnCreatePhysicsSystems(List<ISystem<float>> systems)
    {
        // Sistemas de Lógica de Rede e Input do Cliente
        systems.Add(new NetworkToCommandSystem(World, _playerSpawner));
        systems.Add(new MovementUpdateSystem(World)); // Reconciliação e interpolação remota
        systems.Add(new LocalInputSystem(World));
        systems.Add(new SendInputSystem(World, _playerSpawner));
        systems.Add(new ProcessMovementSystem(World)); // <-- Adiciona o sistema de movimento compartilhado
        
    
        GD.Print("[ClientECS] Sistemas de física do cliente registrados");
    }
}