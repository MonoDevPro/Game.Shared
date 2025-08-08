using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Client.Infrastructure.Adapters;
using Game.Shared.Client.Infrastructure.ECS.Components;
using Game.Shared.Client.Infrastructure.Input;
using Godot;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

/// <summary>
/// Captura o input do jogador, envia a intenção para o servidor E inicia o movimento preditivo localmente.
/// </summary>
public partial class LocalInputSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag, GridPositionComponent>]
    [None<IsMovingTag, MoveIntentCommand>] // Só roda se não estiver se movendo
    private void ProcessInput(in Entity entity)
    {
        var intentVector = Vector2I.Zero;
        
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_UP))    intentVector.Y = -1;
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_DOWN))  intentVector.Y = 1;
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_LEFT))  intentVector.X = -1;
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_RIGHT)) intentVector.X = 1;
        
        if (intentVector != Vector2I.Zero)
        {
            // 1. Adiciona o comando de intenção para ser enviado ao servidor pelo SendInputSystem.
            World.Add(entity, new MoveIntentCommand { Direction = intentVector.ToGridVector() });
        }
    }
}