using Arch.Core;
using Game.Shared.Resources.Loader;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Join;
using Godot;

namespace Game.Shared.Scripts.Shared.ECS.Entities;

/// <summary>
/// Classe base para entidades Godot que se integram com o ECS
/// </summary>
public partial class CharacterScript : CharacterBody2D
{
    private const string SceneDirectory = "res://SharedScenes/Entities/Character.tscn";
    
    // ECS
    public World World { get; private set; }
    public Entity Entity { get; private set; }
    
    // Godot Nodes 
    private CollisionShape2D _collisionShape;
    
    public static CharacterScript Create(World world, PlayerData data)
    {
        // Carrega o recurso de cena do personagem Godot
        var character = new ResourcePath<CharacterScript>(SceneDirectory)
            .Instantiate<CharacterScript>();
        
        character.World = world;

        character.Entity = world.Create(
            new NetworkedTag { Id = data.NetId },
            new PositionComponent { Value = data.Position },
            new VelocityComponent { Value = data.Velocity },
            new SpeedComponent { Value = data.Speed },
            new InputComponent { Value = Vector2.Zero },
            new SceneBodyRefComponent { Value = character }
        );
        
        return character;
    }
    
    public override void _Ready()
    {
        base._Ready();
        base.SetProcess(false); // ECS não usa _Process, então desabilitamos para evitar conflitos
        base.SetPhysicsProcess(true); // ECS reaproveita a lógica de física, então habilitamos o processamento físico
        base.SetProcessInput(false); // ECS não usa _Input, então desabilitamos para evitar conflitos
        base.SetProcessUnhandledInput(false); // ECS não usa _UnhandledInput, então desabilitamos para evitar conflitos
        
        // Inicializa o CollisionShape2D
        _collisionShape = GetNode<CollisionShape2D>(nameof(CollisionShape2D));
    }

    public override void _ExitTree()
    {
        World.Destroy(Entity);
        base._ExitTree();
    }
}
