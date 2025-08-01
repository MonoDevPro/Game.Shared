using Game.Shared.Resources.Client.Sprites.Tools;
using Game.Shared.Resources.Shared.Loader;
using Game.Shared.Scripts.Shared.Enums;
using Godot;

namespace Game.Shared.Resources.Client;

[Tool]
public partial class CharacterSprite : AnimatedSprite2D
{
    [Export] public VocationEnum Vocation
    {
        get => _vocation;
        set
        {
            _vocation = value;
            // Atualiza o SpriteFrames quando a vocação muda
            SpriteFrames = GetSpriteFrames(value, Gender).Load();
            // Reproduz a animação atual após a mudança de vocação
            PlayCurrent();
        }
    }

    [Export] public GenderEnum Gender
    {
        get => _currentGender;
        set
        {
            _currentGender = value;
            // Atualiza o SpriteFrames quando o gênero muda
            SpriteFrames = GetSpriteFrames(Vocation, value).Load();
            // Reproduz a animação atual após a mudança de gênero
            PlayCurrent();
        }
    }

    [Export] public ActionEnum Action { get => _currentAction; set => SetState(value, Direction); }
    [Export] public DirectionEnum Direction { get => _currentDirection; set => SetState(Action, value); }

    // Aqui você expõe o AnimationSet criado antes
    [Export] public AnimationSet Animations { get; set; }
    
    private VocationEnum _vocation = VocationEnum.None;
    private GenderEnum _currentGender = GenderEnum.None;
    private ActionEnum _currentAction = ActionEnum.Idle;
    private DirectionEnum _currentDirection = DirectionEnum.South;

    public static CharacterSprite Create(VocationEnum vocation, GenderEnum gender, AnimationSet animations)
    {
        var inst = new ResourcePath<CharacterSprite>("res://Resources/Client/Sprites/Entries/CharacterSprite.tscn").Load();
        inst.Vocation = vocation;
        inst.Gender = gender;
        inst.Animations = animations;
        return inst;
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
        const string spritesDir = "res://Resources/Client/Sprites/";
        
        var spriteFramesPath = $"{spritesDir}{vocation.ToString()}/{gender.ToString()}/spriteframes.tres";
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
            Play(animName);
        }
        else
        {
            GD.PrintErr($"Animation '{animName}' não existe no SpriteFrames atual.");
        }
    }

    /// <summary>
    /// Chame este método sempre que o personagem mudar de ação ou direção.
    /// </summary>
    public void SetState(ActionEnum action, DirectionEnum direction)
    {
        // evita reprocurar se não mudou nada
        if (action == _currentAction && direction == _currentDirection)
            return;

        _currentAction = action;
        _currentDirection = direction;
        PlayCurrent();
    }
}
