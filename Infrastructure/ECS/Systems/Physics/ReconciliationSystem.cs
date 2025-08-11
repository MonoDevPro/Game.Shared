using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Commands;
using GameClient.Infrastructure.ECS.Components;
using Microsoft.Extensions.Logging;
using Shared.Core.Constants;
using Shared.Core.Extensions;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.Math;

namespace GameClient.Infrastructure.ECS.Systems.Physics;

/// <summary>
/// Sistema exclusivo do cliente, responsável por reconciliar o estado do jogador local
/// com o estado autoritativo recebido do servidor.
/// Se a posição do cliente estiver dessincronizada, este sistema a corrige antes
/// de iniciar o próximo movimento visual.
/// </summary>
public partial class ReconciliationSystem(World world, ILogger<ReconciliationSystem> logger) : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;

    [Query]
    [All<PlayerControllerTag, RemoteMoveIntentCommand>] // Atua apenas no nosso jogador
    [None<MovementStateComponent>] // E apenas se ele não estiver se movendo
    private void Reconcile(in Entity entity, 
        ref GridPositionComponent gridPos, 
        ref DirectionComponent dir, 
        in SpeedComponent speed, 
        in RemoteMoveIntentCommand intent)
    {
        // 1. Reconciliação da Posição
        // Compara a posição atual do cliente com a que o servidor diz ser a correta.
        if (gridPos.Value != intent.GridPosition)
        {
            logger.LogWarning($"Reconciliação! Posição do servidor: {intent.GridPosition}, Posição do cliente: {gridPos.Value}. Corrigindo.");
            gridPos.Value = intent.GridPosition; // Correção "snap". A posição do servidor é a verdade.
        }

        // 2. Início do Movimento (lógica similar ao antigo RemoteMoveSystem)
        // O alvo do movimento é a posição corrigida + a direção do intento.
        GridVector targetGridPos = gridPos.Value + intent.Direction;

        // Atualiza a direção visual do personagem.
        dir.Value = intent.Direction.VectorToDirection();
            
        // Calcula os parâmetros para a interpolação visual.
        var startPixelPos = WorldPosition.FromGridPosition(gridPos.Value, GridSize);
        var targetPixelPos = WorldPosition.FromGridPosition(targetGridPos, GridSize);
            
        var distance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento visual.
        World.Add(entity, new MovementStateComponent
        {
            StartPosition = startPixelPos,
            TargetPosition = targetPixelPos,
            Duration = duration,
            TimeElapsed = 0f
        });
        
        // O comando de intento foi processado e pode ser removido.
        World.Remove<RemoteMoveIntentCommand>(entity);
    }
}