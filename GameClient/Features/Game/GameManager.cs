using GameClient.Core.Common;
using GameClient.Core.Services;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.Network;

namespace GameClient.Features.Game;

public partial class GameManager : Node
{
    // Singleton instance for global access
    public GameServiceProvider Provider;
    public NetworkManager ClientNetwork;
    private EcsRunner _ecsRunner;
    
    public override void _Ready()
    {
        Provider = SingletonAdapter.GetSingleton<GameServiceProvider>();
        ClientNetwork = Provider.Services.GetRequiredService<NetworkManager>();
        _ecsRunner = Provider.Services.GetRequiredService<EcsRunner>();
        
        GodotInputMap.SetupDefaultInputs();
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
        base._ExitTree();
    }
}
