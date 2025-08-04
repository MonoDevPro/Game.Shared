using Game.Shared.Client.Infrastructure.ECS;
using Game.Shared.Client.Infrastructure.Input;
using Game.Shared.Client.Infrastructure.Network;
using Game.Shared.Client.Infrastructure.Spawners;
using Godot;

namespace Game.Shared.Client.Infrastructure.Bootstrap;

public sealed partial class ClientBootstrap : Node
{
    public ClientNetwork ClientNetwork { get; private set; }
    public ClientECS ClientECS { get; private set; }
    public ClientPlayerSpawner ClientPlayerSpawner { get; private set; }
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
        
        ClientNetwork = GetNode<ClientNetwork>(nameof(ClientNetwork));
        ClientECS = GetNode<ClientECS>(nameof(ClientECS));
        ClientPlayerSpawner = GetNode<ClientPlayerSpawner>(nameof(ClientPlayerSpawner));
        
        GD.Print("[Client] Bootstrap complete");
        
        GodotInputMap.SetupDefaultInputs();
        
        base._Ready();
    }
    
    public override void _Process(double delta)
    {
        // Update Process systems
        ClientECS.UpdateProcessSystems((float)delta);
        
        base._Process(delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // 1. Atualiza todos os sistemas de física.
        ClientECS.UpdatePhysicsSystems((float)delta);
    
        // 2. Envia os pacotes enfileirados dos buffers.
        ClientNetwork.Sender.FlushAllBuffers();
        
        base._PhysicsProcess(delta);    
    }
    
    
}
