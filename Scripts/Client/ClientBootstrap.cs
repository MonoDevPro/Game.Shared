using Game.Shared.Scripts.Client.ECS;
using Game.Shared.Scripts.Client.Network;
using Game.Shared.Scripts.Client.Spawners;
using Godot;

namespace Game.Shared.Scripts.Client;

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
        base._Process(delta);
        
        // Update Process systems
        ClientECS.UpdateProcessSystems((float)delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // Update Physics systems
        ClientECS.UpdatePhysicsSystems((float)delta);
    }
    
    
}
