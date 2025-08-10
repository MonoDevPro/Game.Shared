using Godot;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Shared.Core.Constants;
using Shared.Core.Extensions;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Math;
using RemoteMoveIntentCommand = GameClient.Infrastructure.ECS.Commands.RemoteMoveIntentCommand;

// Usa os novos tipos de matemática
namespace GameClient.Infrastructure.ECS.Systems.Physics;

/// <summary>
/// Sistema cliente que lida com as atualizações de estado do servidor.
/// - Para jogadores remotos, inicia a interpolação de movimento (tween).
/// - Para o jogador local, corrige a posição se houver dessincronização.
/// </summary>
public partial class RemoteMoveSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;
    
    // Parte 1: Inicia o movimento. Válido para Cliente (predição) e Servidor (autoridade).
    [Query]
    [All<NetworkedTag, RemoteMoveIntentCommand>]
    [None<MovementStateComponent>]
    private void StartMovement(in Entity entity, 
        ref DirectionComponent dir, ref GridPositionComponent gridPos, in SpeedComponent speed, 
        in RemoteMoveIntentCommand remoteIntent)
    {
        gridPos.Value = remoteIntent.GridPosition;
        GridVector targetGridPos = gridPos.Value + remoteIntent.Direction;

        dir.Value = remoteIntent.Direction.VectorToDirection();
            
        var startPixelPos = new WorldPosition(gridPos.Value.X * GridSize, gridPos.Value.Y * GridSize);
        var targetPixelPos = new WorldPosition(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
            
        var distance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementStateComponent
        {
            StartPosition = startPixelPos,
            TargetPosition = targetPixelPos,
            Duration = duration,
            TimeElapsed = 0f
        });
        
        World.Remove<RemoteMoveIntentCommand>(entity);
    }
}