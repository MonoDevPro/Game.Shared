using Arch.Core;
using Game.Shared.Client.Infrastructure.ECS.Components;
using Game.Shared.Client.Presentation.Entities.Character.Infos;
using Game.Shared.Client.Presentation.Entities.Character.Sprites;
using Game.Shared.Shared.Infrastructure.Loader;
using Godot;
using Shared.Infrastructure.Network.Data.Join;

namespace Game.Shared.Client.Presentation.Entities.Character;

/// <summary>
/// Classe base para entidades Godot que se integram com o ECS
/// </summary>
public partial class PlayerCharacter : CharacterScript
{
    private static string ScenePathString { get; } = "res://Client/Presentation/Entities/Character/PlayerCharacter.tscn";
    private static ResourcePath<PackedScene> ScenePath { get; } = new(ScenePathString);
    
    // Godot Nodes 
    private PlayerInfoDisplay _playerInfoDisplay;
    private CharacterSprite _characterSprite;
    private PlayerData _data;
    
    public static PlayerCharacter CreatePlayer(World world, PlayerData data)
    {
        // Carrega o recurso de cena do personagem Godot
        var character = ScenePath.Instantiate<PlayerCharacter>();
        
        character.Initialize(world, data);
        
        return character;
    }
    
    protected override void Initialize(World world, PlayerData data)
    {
        base.Initialize(world, data);
        
        _data = data;
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        _playerInfoDisplay = GetNode<PlayerInfoDisplay>(nameof(PlayerInfoDisplay)); 
        _characterSprite = GetNode<CharacterSprite>(nameof(CharacterSprite));

        if (_characterSprite == null)
        {
            GD.PrintErr("[PlayerCharacter] CharacterSprite node not found!");
            return;
        }

        if (_playerInfoDisplay == null)
        {
            GD.PrintErr("[PlayerCharacter] PlayerInfoDisplay node not found!");
            return;
        }
        
        _playerInfoDisplay.UpdateInfo(_data.Name);
        _characterSprite.Vocation = _data.Vocation;
        _characterSprite.Gender = _data.Gender;
        
        // Adiciona o componente de sprite de animação ao cliente
        var spriteRef = new SpriteRefComponent { Value = _characterSprite };
        World.Add(Entity, spriteRef);
    }
}
