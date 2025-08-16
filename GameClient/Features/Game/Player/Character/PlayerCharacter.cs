using Game.Core.ECS.Components;
using Game.Core.Entities.Common.Constants;
using Game.Core.Entities.Common.Enums;
using Game.Core.Entities.Common.ValueObjetcs;
using GameClient.Core.Loader;
using GameClient.Features.Game.Player.Character.Infos;
using GameClient.Features.Game.Player.Character.Sprites;
using Godot;

namespace GameClient.Features.Game.Player.Character;

public partial class PlayerCharacter : Node2D
{
    private const string CharacterScenePath = "res://Features/Game/Player/Character/PlayerCharacter.tscn";

    public record InitializationData(
        CharInfoComponent Info,
        MapPositionComponent GridPosition,
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
        var worldPosition = GameClient.Core.Common.GridToWorld.ToWorld(data.GridPosition.Value);
        playerCharacter.GlobalPosition = new Godot.Vector2(worldPosition.X, worldPosition.Y);

        playerCharacter.CharacterSprite = CharacterSprite.Create(data.Info.Vocation, data.Info.Gender);
        playerCharacter.GetNode("Pivot").AddChild(playerCharacter.CharacterSprite);
        playerCharacter.Initialize(data);

        return playerCharacter;
    }

    /// <summary>
    /// Recebe os dados de inicialização do sistema que o criou.
    /// É chamado antes do _Ready().
    /// </summary>
    private void Initialize(InitializationData data)
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

            CharacterSprite.SetState(
                ActionEnum.Idle,
                _initializationData.Direction.Value,
                _initializationData.Speed.Value);
        }

        base._Ready();
    }
}