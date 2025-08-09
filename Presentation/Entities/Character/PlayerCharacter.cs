using GameClient.Presentation.Entities.Character.Infos;
using GameClient.Presentation.Entities.Character.Sprites;
using Godot;
using Shared.Infrastructure.ECS.Components;

namespace GameClient.Presentation.Entities.Character;

public partial class PlayerCharacter : CharacterBody2D
{
    // Godot Nodes 
    public CollisionShape2D CollisionShape { get; private set; }
    public CharacterSprite CharacterSprite { get; private set; }
    public PlayerInfoDisplay PlayerInfoDisplay { get; private set; }

    // Variável para guardar os dados recebidos antes do _Ready()
    private PlayerInfoComponent _initializationData;
    private bool _isInitialized = false;

    /// <summary>
    /// Recebe os dados de inicialização do sistema que o criou.
    /// É chamado antes do _Ready().
    /// </summary>
    public void Initialize(PlayerInfoComponent playerInfo)
    {
        _initializationData = playerInfo;
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
        CollisionShape = GetNode<CollisionShape2D>(nameof(CollisionShape2D));
        PlayerInfoDisplay = GetNode<PlayerInfoDisplay>(nameof(PlayerInfoDisplay)); 
        CharacterSprite = GetNode<CharacterSprite>(nameof(CharacterSprite));
        
        // Agora que temos a certeza de que CharacterSprite e PlayerInfoDisplay não são nulos,
        // usamos os dados guardados para os configurar.
        if (_isInitialized)
        {
            PlayerInfoDisplay.UpdateInfo(_initializationData.Name);
            CharacterSprite.AddVocationAndGender(
                _initializationData.Vocation, 
                _initializationData.Gender
            );
        }
        
        base._Ready();
    }
}