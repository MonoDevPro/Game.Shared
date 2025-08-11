using GameClient.Infrastructure.Adapters;
using GameClient.Infrastructure.Loader;
using GameClient.Presentation.Entities.Character.Infos;
using GameClient.Presentation.Entities.Character.Sprites;
using Godot;
using Shared.Core.Constants;
using Shared.Core.Enums;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.Math;

namespace GameClient.Presentation.Entities.Character;

public partial class PlayerCharacter : Node2D
{
    private const string CharacterScenePath = "res://Presentation/Entities/Character/PlayerCharacter.tscn";
    
    public record InitializationData(
        PlayerInfoComponent Info,
        GridPositionComponent GridPosition,
        DirectionComponent Direction,
        SpeedComponent Speed);
    
    // Godot Nodes 
    public CharacterSprite CharacterSprite { get; private set; }
    public PlayerInfoDisplay PlayerInfoDisplay { get; private set; }

    // Variável para guardar os dados recebidos antes do _Ready()
    private InitializationData _initializationData;
    private bool _isInitialized = false;

    private static readonly ResourcePath<PackedScene> ScenePath =
        new(CharacterScenePath);
    
    public static PlayerCharacter Create(InitializationData data)
    {
        var playerCharacter = ScenePath.Instantiate<PlayerCharacter>();
        var worldPosition = WorldPosition.FromGridPosition(data.GridPosition.Value, GameMapConstants.GridSize);
        playerCharacter.GlobalPosition = worldPosition.ToGodotVector2();
        
        playerCharacter.CharacterSprite = CharacterSprite.Create(data.Info.Vocation, data.Info.Gender);
        playerCharacter.GetNode("Pivot").AddChild(playerCharacter.CharacterSprite);
        playerCharacter.CharacterSprite.CallDeferred(CharacterSprite.MethodName.SetState, 
            (int)ActionEnum.Idle, 
            (int)data.Direction.Value, 
            (int)data.Speed.Value);
        
        playerCharacter.Initialize(data);
        
        return playerCharacter;
    }

    /// <summary>
    /// Recebe os dados de inicialização do sistema que o criou.
    /// É chamado antes do _Ready().
    /// </summary>
    public void Initialize(InitializationData data)
    {
        _initializationData = data;
        _isInitialized = true;
    }
    
    public override void _Ready()
    {
        // A lógica de desativar os processos continua a mesma...
        base.SetProcess(false); // ECS não usa _Process, então desabilitamos para evitar conflitos
        base.SetPhysicsProcess(true); // ECS reaproveita a lógica de física, então habilitamos o processamento físico
        base.SetProcessInput(false); // ECS não usa _Input, então desabilitamos para evitar conflitos
        base.SetProcessUnhandledInput(false); // ECS não usa _UnhandledInput, então desabilitamos para evitar conflitos
        
        // Obtém as referências aos nós filhos como antes
        PlayerInfoDisplay = GetNode<PlayerInfoDisplay>("Pivot/PlayerInfoDisplay");
        
        // Agora que temos a certeza de que CharacterSprite e PlayerInfoDisplay não são nulos,
        // usamos os dados guardados para os configurar.
        if (_isInitialized)
        {
            var info = _initializationData.Info;
            PlayerInfoDisplay.UpdateInfo(info.Name);
        }
        
        base._Ready();
    }
}