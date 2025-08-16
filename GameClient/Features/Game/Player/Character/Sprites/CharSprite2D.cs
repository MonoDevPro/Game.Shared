using Game.Core.Entities.Common.Enums;
using Godot;

namespace GameClient.Features.Game.Player.Character.Sprites;

[Tool]
public partial class CharacterSprite2D : Node2D
{
    private const string SpritePath = "res://Features/Game/Player/Character/Sprites/";
    private const float BaseMovementSpeedForAnimation = 40.0f;

    #region Nós Filhos (Configure no Inspetor ou pegue no _Ready)
    [Export] private Sprite2D _sprite;
    [Export] private AnimationPlayer _animationPlayer;
    [Export] private AnimationTree _animationTree;
    #endregion
    
    #region Propriedades (O que controla o estado)
    private VocationEnum _vocation;
    [Export]
    public VocationEnum Vocation
    {
        get => _vocation;
        set { _vocation = value; UpdateAnimationLibrary(); }
    }

    private GenderEnum _gender;
    [Export]
    public GenderEnum Gender
    {
        get => _gender;
        set { _gender = value; UpdateAnimationLibrary(); }
    }
    #endregion

    public override void _Ready()
    {
        // Garante que a referência ao AnimationTree esteja ativa para a máquina de estados
        if (_animationTree != null)
        {
            _animationTree.Active = true;
        }
    }

    /// <summary>
    /// Carrega a biblioteca de animações correta para a vocação e gênero atuais.
    /// </summary>
    private void UpdateAnimationLibrary()
    {
        if (_vocation == VocationEnum.None || _gender == GenderEnum.None || _animationPlayer == null)
            return;

        // Agora carregamos uma AnimationLibrary em vez de SpriteFrames
        var animLibraryPath = $"{SpritePath}{_vocation.ToString()}/{_gender.ToString()}/animation_library.anim";
        var animLibrary = GD.Load<AnimationLibrary>(animLibraryPath);
    
        if (animLibrary != null)
        {
            // Remove a biblioteca antiga para evitar conflitos de nomes
            if (_animationPlayer.HasAnimationLibrary("character"))
            {
                _animationPlayer.RemoveAnimationLibrary("character");
            }
            // Adiciona a nova biblioteca
            _animationPlayer.AddAnimationLibrary("character", animLibrary);
        }
        else
        {
            GD.PrintErr($"AnimationLibrary não encontrada em: {animLibraryPath}");
        }
    }

    /// <summary>
    /// Define o estado visual do sprite, ajustando os parâmetros do AnimationTree.
    /// Este método é agora a principal forma de controlar a animação.
    /// </summary>
    /// <param name="action">A ação atual (Idle, Walk, etc.)</param>
    /// <param name="direction">A direção para a qual olhar.</param>
    /// <param name="movementSpeed">A velocidade de movimento atual da entidade.</param>
    public void SetState(ActionEnum action, DirectionEnum direction, float movementSpeed)
    {
        if (_animationTree == null) return;
        
        // 1. Define a velocidade da animação (blend)
        // O AnimationTree pode ter um parâmetro "TimeScale" para controlar a velocidade geral.
        float speedScale = movementSpeed > 0.1f ? movementSpeed / BaseMovementSpeedForAnimation : 1.0f;
        _animationTree.Set("parameters/TimeScale/scale", speedScale);

        // 2. Define a direção para o BlendSpace2D
        // O AnimationTree terá um BlendSpace2D para "idle" e outro para "walk".
        // Ambos usarão o mesmo parâmetro de direção para funcionar.
        _animationTree.Set("parameters/blend_direction", DirectionToVector(direction));

        // 3. Define a transição na Máquina de Estados
        // O AnimationTree terá uma máquina de estados com transições como "walk", "idle", "attack".
        if (action == ActionEnum.Walk)
        {
            _animationTree.Set("parameters/conditions/is_walking", true);
            _animationTree.Set("parameters/conditions/is_idling", false);
        }
        else if (action == ActionEnum.Idle)
        {
            _animationTree.Set("parameters/conditions/is_walking", false);
            _animationTree.Set("parameters/conditions/is_idling", true);
        }
        // Exemplo para um ataque que é um "one-shot"
        else if (action == ActionEnum.Attack)
        {
             _animationTree.Set("parameters/conditions/do_attack", true);
        }
    }
    
    /// <summary>
    /// Converte nosso DirectionEnum em um Vector2 para o BlendSpace2D.
    /// </summary>
    private Vector2 DirectionToVector(DirectionEnum direction)
    {
        return direction switch
        {
            DirectionEnum.North => Vector2.Up,
            DirectionEnum.South => Vector2.Down,
            DirectionEnum.East => Vector2.Right,
            DirectionEnum.West => Vector2.Left,
            DirectionEnum.NorthEast => (Vector2.Up + Vector2.Right).Normalized(),
            DirectionEnum.NorthWest => (Vector2.Up + Vector2.Left).Normalized(),
            DirectionEnum.SouthEast => (Vector2.Down + Vector2.Right).Normalized(),
            DirectionEnum.SouthWest => (Vector2.Down + Vector2.Left).Normalized(),
            _ => Vector2.Down
        };
    }
}