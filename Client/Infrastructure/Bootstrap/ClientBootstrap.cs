using Arch.Core;
using Game.Shared.Client.Infrastructure.ECS;
using Game.Shared.Client.Infrastructure.Input;
using Game.Shared.Client.Infrastructure.Network;
using Game.Shared.Client.Infrastructure.Spawners;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.ECS;

namespace Game.Shared.Client.Infrastructure.Bootstrap;

public sealed partial class ClientBootstrap : Node
{
    public GameServiceProvider ServiceProvider { get; private set; } = new GameServiceProvider();
    public ClientNetwork ClientNetwork { get; private set; }
    public EcsRunner ClientECS { get; private set; }
    public ClientPlayerSpawner ClientPlayerSpawner { get; private set; }
    public World World { get; private set; }
    public static ClientBootstrap Instance { get; private set; }
    
    public override void _Ready()
    {
        // Ensure this is a singleton instance
        if (Instance != null)
        {
            GD.PrintErr("[GameRoot] Instance already exists. This should be a singleton.");
            return;
        }
        Instance = this;
        GD.Print("[Client] Bootstrap complete");
        GodotInputMap.SetupDefaultInputs();
        
        // Initialize the service provider
        AddChild(ServiceProvider);
        
        // Initialize the network manager
        ClientNetwork = ServiceProvider.Services.GetRequiredService<ClientNetwork>();
        ClientECS = ServiceProvider.Services.GetRequiredService<EcsRunner>();
        World = ServiceProvider.Services.GetRequiredService<World>();
        ClientPlayerSpawner = ServiceProvider.Services.GetRequiredService<ClientPlayerSpawner>();
        
        base._Ready();
    }
    
    public override void _Process(double delta)
    {
        // Update Process systems
        ClientECS.UpdateProcess((float)delta);
        
        base._Process(delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 1. Atualiza todos os sistemas de física.
        ClientECS.UpdatePhysics((float)delta);
    
        // 2. Envia os pacotes enfileirados dos buffers.
        ClientNetwork.Sender.FlushAllBuffers();
        
        base._PhysicsProcess(delta);    
    }
    
    
}
