using Arch.Core;
using GameClient.Infrastructure.Adapters;
using GameClient.Infrastructure.DI;
using GameClient.Infrastructure.Events;
using GameClient.Infrastructure.Input;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.ECS;
using Shared.Infrastructure.Network;

namespace GameClient;

public sealed partial class ClientBootstrap : Node
{
    // Singleton instance for global access
    public static ClientBootstrap Instance { get; private set; }
    public GameServiceProvider Provider = new();
    public NetworkManager ClientNetwork;
    private EcsRunner _ecsRunner;
    
    public override void _Ready()
    {
        Instance ??= this;
        

        Provider = SingletonAdapter.GetSingleton<GameServiceProvider>();
        ClientNetwork = Provider.Services.GetRequiredService<NetworkManager>();
        _ecsRunner = Provider.Services.GetRequiredService<EcsRunner>();
        GodotInputMap.SetupDefaultInputs();
        
        NetworkEvents.OnDisconnectedFromServer += OnServerDisconnected;
        NetworkEvents.OnConnectedToServer += OnServerConnected;
        GameEvents.OnGameStarted += OnGameStarted; // <-- Jogador Local entrou no jogo
        
        base._Ready();
        
        GD.Print("[Client] Bootstrap complete");
    }
    
    public void ConnectToServer()
    {
        if (ClientNetwork.IsRunning)
        {
            GD.Print("[Client] Already connected to server, skipping connection.");
            return;
        }
        
        ClientNetwork.Start();
    }
    
    private void OnServerConnected()
    {
        GetNode<Window>("%CreateCharacter").Show();
        GetNode<Window>("%GameLogin").Hide();
    }
    private void OnServerDisconnected()
    {
        GetTree().Quit(1);
    }
    
    private void OnGameStarted()
    {
        // Agora sim, o jogo começou de verdade.
        // Esconde a janela de criação de personagem e mostra a UI principal do jogo.
        GetNode<Window>("%CreateCharacter").Hide();
        GetNode<Control>("%GameUI").Show();
    }
    
    // O Process e o PhysicsProcess agora fazem a mesma coisa:
    // simplesmente dizem ao EcsRunner para executar um tick completo.
    public override void _PhysicsProcess(double delta)
    {
        float deltaSeconds = (float)delta;
        _ecsRunner.BeforeUpdate(deltaSeconds);
        _ecsRunner.Update(deltaSeconds);
        _ecsRunner.AfterUpdate(deltaSeconds);
    }
    
    // Pode remover o _Process ou mantê-lo para lógica que não seja do ECS, se necessário.
    public override void _Process(double delta) { }
    
    public override void _ExitTree()
    {
        ClientNetwork.Dispose();
        Provider.Dispose();
        _ecsRunner.Dispose();
        NetworkEvents.OnDisconnectedFromServer -= OnServerDisconnected;
        NetworkEvents.OnConnectedToServer -= OnServerConnected;
        base._ExitTree();
    }
    
}
