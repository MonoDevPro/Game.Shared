using Game.Shared.Server.Infrastructure.ECS;
using Game.Shared.Server.Infrastructure.Network;
using Game.Shared.Server.Infrastructure.Spawners;
using Godot;

namespace Game.Shared.Server.Infrastructure.Bootstrap;

public sealed partial class ServerBootstrap : Node
{
    public ServerNetwork ServerNetwork { get; private set; }
    public ServerECS ServerECS { get; private set; }
    public ServerPlayerSpawner PlayerSpawner { get; private set; }
    public static ServerBootstrap Instance { get; private set; }  /// --> Singleton instance for easy access

    public override void _Ready()
    {
        // Ensure this is a singleton instance
        if (Instance != null)
        {
            GD.PrintErr("[GameRoot] Instance already exists. This should be a singleton.");
            QueueFree(); // Remove this instance if it already exists
            return;
        }
        
        Instance = this;
        
        ServerNetwork = GetNode<ServerNetwork>(nameof(ServerNetwork));
        PlayerSpawner = GetNode<ServerPlayerSpawner>(nameof(ServerPlayerSpawner));
        ServerECS = GetNode<ServerECS>(nameof(ServerECS));
        
        // Start the ECS and Network systems
        ServerNetwork.Start();
        
        GD.Print("[Server] Bootstrap complete");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Update Process systems
        ServerECS.UpdateProcessSystems((float)delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // 1. Atualiza todos os sistemas de física.
        ServerECS.UpdatePhysicsSystems((float)delta);
    
        // 2. Envia os pacotes enfileirados dos buffers.
        ServerNetwork.Sender.FlushAllBuffers();
    }
}
