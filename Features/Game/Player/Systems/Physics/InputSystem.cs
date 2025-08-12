using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.Common;
using GameClient.Core.Services;
using GameClient.Features.Player.Components;
using Godot;
using Shared.Core.Common.Helpers;
using Shared.Features.Player.Components;
using Shared.Features.Player.Components.Commands;

namespace GameClient.Features.Player.Systems.Physics;

/// <summary>
/// Captura o input do jogador, envia a intenção para o servidor E inicia o movimento preditivo localmente.
/// </summary>
public partial class InputSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag>]
    [None<MoveIntentCommand, MovementProgressComponent>] // Só roda se não estiver se movendo
    private void ProcessMoveInput(in Entity entity)
    {
        var intentVector = Vector2I.Zero;
        
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_UP))    intentVector.Y = -1;
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_DOWN))  intentVector.Y = 1;
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_LEFT))  intentVector.X = -1;
        if (Godot.Input.IsActionPressed(GodotInputMap.MOVE_RIGHT)) intentVector.X = 1;

        if (intentVector != Vector2I.Zero)
            World.Add(entity, new MoveIntentCommand { Direction = intentVector.ToGridVector() });
    }
    
    [Query]
    [All<PlayerControllerTag>]
    [None<AttackIntentCommand, AttackProgressComponent>] // Só roda se não estiver atacando
    public void ProcessAttackInput(in Entity entity, in DirectionComponent direction)
    {
        if (Godot.Input.IsActionPressed(GodotInputMap.ATTACK))
            World.Add(entity, new AttackIntentCommand { Direction = direction.Value.ToGridVector() });
    }
}