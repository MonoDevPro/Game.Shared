using Arch.Core;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Loader;
using Game.Shared.Shared.Infrastructure.Network.Data.Join;
using Godot;

namespace Game.Shared.Shared.Entities;

/// <summary>
/// Classe base para entidades Godot que se integram com o ECS
/// </summary>
public partial class CharacterScript : CharacterBody2D
{
    public static ResourcePath<PackedScene> ScenePath { get; } = new("res://Shared/Entities/Character.tscn");
    
    private const int GridSize = 32;
    private const float DefaultSpeed = 900.0f; // Velocidade padrão em pixels/segundo
    
    // ECS
    public World World { get; private set; }
    public Entity Entity { get; private set; }
    
    // Godot Nodes 
    private CollisionShape2D _collisionShape;
    
    public static CharacterScript Create(World world, PlayerData data)
    {
        // Carrega o recurso de cena do personagem Godot
        var character = ScenePath.Instantiate<CharacterScript>();
        
        character.World = world;

        // Calcula a posição inicial em pixels a partir da posição do grid
        Vector2 initialPixelPosition = new Vector2(data.GridPosition.X * GridSize, data.GridPosition.Y * GridSize);

        // Define a posição visual inicial do nó Godot
        character.GlobalPosition = initialPixelPosition;

        // Cria a entidade ECS com os componentes corretos
        character.Entity = world.Create(
            new NetworkedTag { Id = data.NetId },
            new PositionComponent { Value = initialPixelPosition },
            new GridPositionComponent { Value = data.GridPosition },
            new SpeedComponent { Value = DefaultSpeed }, // Adiciona o componente de velocidade
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
