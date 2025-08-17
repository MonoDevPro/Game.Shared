using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.ECS.Components;
using GameClient.Core.ECS.Components.States;
using Microsoft.Extensions.Logging;

namespace GameClient.Core.ECS.Systems;

public partial class MovementProcessSystem(World world, ILogger<MovementProcessSystem> logger)
    : BaseSystem<World, float>(world)
{
    // Parte 2: Processa o progresso do movimento.
    [Query]
    [All<MovementProgressComponent>]
    private void ProcessMovement([Data] float delta, in Entity entity, ref MapPositionComponent gridPos, ref MovementProgressComponent moveState)
    {
        moveState.TimeElapsed += delta;

        // Quando o tempo do movimento termina...
        if (moveState.TimeElapsed >= moveState.Duration)
        {
            // ...o estado lógico é atualizado para a posição final.
            // Isso acontece de forma idêntica no cliente e no servidor.
            gridPos.Value = moveState.TargetPosition;

            // O movimento terminou, removemos o componente de estado.
            World.Remove<MovementProgressComponent>(entity);
        }
    }
}