using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Godot;

namespace Game.Shared.Scripts.Client.ECS.Systems;

/// <summary>
/// Captura o input do jogador e cria uma intenção de movimento.
/// Só permite um novo movimento se o jogador não estiver se movendo.
/// </summary>
public partial class LocalInputSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag>]
    [None<IsMovingTag, MoveIntentCommand>] // Só roda se não estiver se movendo ou com uma intenção pendente
    private void ProcessInput(in Entity entity)
    {
        var intent = Vector2I.Zero;
        
        if (Input.IsActionPressed(GodotInputMap.MOVE_UP))    intent.Y = -1;
        if (Input.IsActionPressed(GodotInputMap.MOVE_DOWN))  intent.Y = 1;
        if (Input.IsActionPressed(GodotInputMap.MOVE_LEFT))  intent.X = -1;
        if (Input.IsActionPressed(GodotInputMap.MOVE_RIGHT)) intent.X = 1;
        
        if (intent != Vector2I.Zero)
        {
            // Adiciona o comando de intenção e a tag de movimento para bloquear novos inputs.
            World.Add(entity, new MoveIntentCommand { Direction = intent });
            World.Add<IsMovingTag>(entity);
        }
    }
}
