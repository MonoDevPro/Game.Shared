using Arch.Core;
using Game.Shared.Resources.Shared.Loader;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Join;
using Godot;

namespace Game.Shared.Scripts.Shared.ECS.Entities;

/// <summary>
/// Classe base para entidades Godot que se integram com o ECS
/// </summary>
public partial class CharacterScript : CharacterBody2D
{
    private const string SceneDirectory = "res://Scenes/Shared/Entities/Character.tscn";
    private const int GridSize = 32;
    
    // ECS
    public World World { get; private set; }
    public Entity Entity { get; private set; }
    
    // Godot Nodes 
    private CollisionShape2D _collisionShape;
    
    public static CharacterScript Create(World world, PlayerData data)
    {
        // Carrega o recurso de cena do personagem Godot
        var character = new ResourcePath<PackedScene>(SceneDirectory)
            .Instantiate<CharacterScript>();
        
        character.World = world;

        // Calcula a posição inicial em pixels a partir da posição do grid
        Vector2 initialPixelPosition = new Vector2(data.GridPosition.X * GridSize, data.GridPosition.Y * GridSize);

        // Define a posição visual inicial do nó Godot
        character.GlobalPosition = initialPixelPosition;

        // Cria a entidade ECS com os componentes corretos
        character.Entity = world.Create(
            new NetworkedTag { Id = data.NetId },
            new GridPositionComponent { Value = data.GridPosition },
            new PositionComponent { Value = initialPixelPosition },
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
