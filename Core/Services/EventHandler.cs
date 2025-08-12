using System;
using Arch.Bus;
using GameClient.Features.Player.Events;
using Godot;
using LiteNetLib;

namespace GameClient.Core.Services;

public partial class EventHandler : IDisposable
{
    private readonly Node _rootNode;
    public EventHandler(Node rootNode)
    {
        Hook();
        _rootNode = rootNode;
    }

    [Event]
    public void OnPlayerSpawned(in LocalPlayerSpawnedEvent localPlayerSpawnedEvent)
    {
        // Agora sim, o jogo começou de verdade.
        // Esconde a janela de criação de personagem e mostra a UI principal do jogo.
        _rootNode.GetNode<Window>("%CreateCharacter").Hide();
        _rootNode.GetNode<Window>("%GameLogin").Hide();
        _rootNode.GetNode<Control>("%GameUI").Show();
        GD.Print($"Local Player Spawned!)");
    }

    [Event]
    public void OnPlayerDespawned(in LocalPlayerDespawnedEvent localPlayerDespawnedEvent)
    {
        // Aqui você pode adicionar lógica para lidar com o evento de jogador despawnado.
        // Por exemplo, atualizar a UI, limpar recursos, etc.
        _rootNode.GetNode<Control>("%GameUI").Hide();
        _rootNode.GetNode<Window>("%CreateCharacter").Hide();
        _rootNode.GetNode<Window>("%GameLogin").Show();
        GD.Print($"Local Player Despawned!)");
    }
    
    [Event]
    public void OnServerConnected(in ServerConnectedEvent @event)
    {
        _rootNode.GetNode<Window>("%CreateCharacter").Show();
        _rootNode.GetNode<Window>("%GameLogin").Hide();
    }
    
    [Event]
    public void OnServerDisconnected(in ServerDisconnectedEvent @event)
    {
        // Aqui você pode adicionar lógica para lidar com a desconexão do servidor.
        // Por exemplo, mostrar uma mensagem de erro ou retornar à tela de login.
        GD.Print($"Server disconnected: {@event.Reason}");
        _rootNode.GetNode<Window>("%CreateCharacter").Hide();
        _rootNode.GetNode<Window>("%GameLogin").Show();
        _rootNode.GetNode<Control>("%GameUI").Hide();
    }

    public void Dispose()
    {
        _rootNode?.Dispose();
        Unhook();
    }
}