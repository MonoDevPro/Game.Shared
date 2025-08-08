using Arch.Core;
using Game.Shared.Client.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Loader;
using Godot;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Network.Data.Join;

namespace Game.Shared.Client.Presentation.Entities.Character;

/// <summary>
/// Classe base para entidades Godot que se integram com o ECS
/// </summary>
public partial class CharacterScript : CharacterBody2D
{
    private static string ScenePathString { get; set; } = "res://Shared/Entities/Character.tscn";
    private static ResourcePath<PackedScene> ScenePath { get; } = new(ScenePathString);
    
    private const int GridSize = 32;
    
    // ECS
    public World World { get; private set; }
    public Entity Entity { get; private set; }
    
    // Godot Nodes 
    private CollisionShape2D _collisionShape;
    
    protected virtual void Initialize(World world, PlayerData data)
    {
        World = world;
        
        // Calcula a posição inicial em pixels a partir da posição do grid
        Vector2 initialPixelPosition = new Vector2(data.GridPosition.X * GridSize, data.GridPosition.Y * GridSize);
        
        this.Position += new Vector2(16, 16); // Ajuste
        
        // Define a posição visual inicial do nó Godot
        this.GlobalPosition = initialPixelPosition;

        // Cria a entidade ECS com os componentes corretos
        this.Entity = world.Create(
            new NetworkedTag { Id = data.NetId },
            new PlayerInfoComponent
            {
                Name = data.Name,
                Vocation = data.Vocation,
                Gender = data.Gender,
            },
            new GridPositionComponent { Value = data.GridPosition },
            new SpeedComponent { Value = data.Speed }, // Adiciona o componente de velocidade
            new DirectionComponent { Value = data.Direction }, // Direção inicial
            new SceneBodyRefComponent { Value = this }
        );
    }

    public static CharacterScript Create(World world, PlayerData data)
    {
        // Carrega o recurso de cena do personagem Godot
        var character = ScenePath.Instantiate<CharacterScript>();
        
        character.Initialize(world, data);
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