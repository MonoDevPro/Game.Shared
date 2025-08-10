using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Shared.Core.Constants;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.ECS.Systems;

public partial class MovementProcessSystem(World world, ILogger<MovementProcessSystem> logger) 
    : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;
    
    // Parte 2: Processa o progresso do movimento.
    [Query]
    [All<MovementStateComponent>]
    private void ProcessMovement([Data] float delta, in Entity entity, ref GridPositionComponent gridPos, ref MovementStateComponent moveState)
    {
        moveState.TimeElapsed += delta;

        // Quando o tempo do movimento termina...
        if (moveState.TimeElapsed >= moveState.Duration)
        {
            // ...o estado lógico é atualizado para a posição final.
            // Isso acontece de forma idêntica no cliente e no servidor.
            gridPos.Value = GridVector.FromWorldPosition(moveState.TargetPosition, GridSize);
            
            // O movimento terminou, removemos o componente de estado.
            World.Remove<MovementStateComponent>(entity);
        }
    }
}