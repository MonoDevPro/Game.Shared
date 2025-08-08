using Game.Shared.Shared.Infrastructure.Loader;
using Godot;
using Shared.Core.Enums;

namespace Game.Shared.Client.Presentation.Entities.Character.Sprites;

[Tool]
public partial class CharacterSprite : AnimatedSprite2D
{
    private const string SpritePath = "res://Client/Presentation/Entities/Character/Sprites/";
    
    // A velocidade de movimento para a qual a sua animação foi desenhada para parecer "normal".
    // Ajuste este valor. Se a sua animação de caminhada parece correta para um personagem
    // a mover-se a 150 pixels/segundo, defina este valor para 150.
    private const float BaseMovementSpeedForAnimation = 40.0f;
    
    public static ResourcePath<PackedScene> ScenePath = 
        new ($"{SpritePath}Entries/CharacterSprite.tscn");
    
    [Export] public VocationEnum Vocation
    {
        get => _vocation;
        set
        {
            _vocation = value;
            
            if (Gender == GenderEnum.None || Vocation == VocationEnum.None)
                return; // Não faz nada se a vocação não estiver definida
            
            SpriteFrames = GetSpriteFrames(value, Gender).Load();
        }
    }

    [Export] public GenderEnum Gender
    {
        get => _currentGender;
        set
        {
            _currentGender = value;
            
            if (Gender == GenderEnum.None || Vocation == VocationEnum.None)
                return; // Não faz nada se a vocação não estiver definida
            
            // Atualiza o SpriteFrames quando o gênero muda
            SpriteFrames = GetSpriteFrames(Vocation, value).Load();
        }
    }

    [Export] public ActionEnum Action { get => _currentAction; set => SetState(value, Direction, _currentMovementSpeed); }
    [Export] public DirectionEnum Direction { get => _currentDirection; set => SetState(Action, value, _currentMovementSpeed); }

    // Aqui você expõe o AnimationSet criado antes
    [Export] public AnimationSet Animations { get; set; }

    private VocationEnum _vocation;
    private GenderEnum _currentGender;
    private ActionEnum _currentAction = ActionEnum.Idle;
    private DirectionEnum _currentDirection = DirectionEnum.South;
    private float _currentMovementSpeed = BaseMovementSpeedForAnimation;

    public static CharacterSprite Create(VocationEnum vocation, GenderEnum gender)
    {
        GD.PrintErr(gender);
        
        var instance = CharacterSprite.ScenePath.Instantiate<CharacterSprite>();
        
        instance._vocation = vocation;
        instance._currentGender = gender;
        return instance;
    }

    public override void _Ready()
    {
        if (Vocation == VocationEnum.None || Gender == GenderEnum.None)
        {
            GD.PrintErr("Vocation ou Gender não podem ser None.");
            return;
        }
        
        if (Animations == null)
        {
            GD.PrintErr("Animations não pode ser nulo. Por favor, atribua um AnimationSet.");
            return;
        }
        
        Animations._Ready();

        // carrega o SpriteFrames padrão pro par (vocation, gender)
        SpriteFrames = GetSpriteFrames(Vocation, Gender).Load();

        // já toca a animação inicial
        PlayCurrent();
    }
    
    private ResourcePath<SpriteFrames> GetSpriteFrames(VocationEnum vocation, GenderEnum gender)
    {
        var spriteFramesPath = $"{SpritePath}{vocation.ToString()}/{gender.ToString()}/spriteframes.tres";
        
        return new ResourcePath<SpriteFrames>(spriteFramesPath);
    }
        
    /// <summary>
    /// Atualiza e toca a animação de acordo com o estado e direção atuais.
    /// </summary>
    public void PlayCurrent()
    {
        // obtém o name configurado no AnimationSet
        var animName = Animations.GetAnimation(Action, Direction);
            
        if (SpriteFrames.HasAnimation(animName))
        {
            // Apenas reinicia a animação se o nome dela mudar.
            if (this.Animation != animName)
            {
                Play(animName);
            }
        }
        else
        {
            GD.PrintErr($"Animation '{animName}' não existe no SpriteFrames atual.");
        }
    }

    /// <summary>
    /// Chame este método sempre que o personagem mudar de ação ou direção.
    /// </summary>
    /// <summary>
    /// Define o estado visual do sprite, ajustando a animação e a sua velocidade.
    /// </summary>
    /// <param name="action">A ação atual (Idle, Walk, etc.)</param>
    /// <param name="direction">A direção para a qual olhar.</param>
    /// <param name="movementSpeed">A velocidade de movimento atual da entidade.</param>
    public void SetState(ActionEnum action, DirectionEnum direction, float movementSpeed)
    {
        // Se a ação for caminhar, a velocidade da animação é proporcional à velocidade de movimento.
        // Caso contrário (ex: Idle, Attack), a animação corre à velocidade normal (escala 1.0).
        this.SpeedScale = (action == ActionEnum.Walk)
            ? movementSpeed / BaseMovementSpeedForAnimation
            : 1.0f;

        // Evita trabalho desnecessário se nada mudou.
        if (action == _currentAction && direction == _currentDirection)
            return;

        _currentAction = action;
        _currentDirection = direction;
        _currentMovementSpeed = movementSpeed;
        
        PlayCurrent();
    }
}