using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.Math;

// Usa os novos tipos de matemática

namespace GameClient.Infrastructure.ECS.Systems.Physics;

/// <summary>
/// Sistema cliente que lida com as atualizações de estado do servidor.
/// - Para jogadores remotos, inicia a interpolação de movimento (tween).
/// - Para o jogador local, corrige a posição se houver dessincronização.
/// </summary>
public partial class MovementUpdateSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;

    [Query]
    [All<GridPositionComponent, MovementUpdateCommand>]
    private void HandleMovementUpdate(in Entity entity, ref GridPositionComponent gridPos, in MovementUpdateCommand update)
    {
        // A posição alvo no mundo visual, usando o nosso tipo de domínio
        var targetVisualPos = new WorldPosition(
            update.NewGridPosition.X * GridSize,
            update.NewGridPosition.Y * GridSize);

        // Atualiza a posição lógica do grid imediatamente
        gridPos.Value = update.NewGridPosition;

        // Apenas proxies remotos usam tweening para suavização.
        var bodyRef = World.Get<SceneBodyRefComponent>(entity);
        ref var speed = ref World.Get<SpeedComponent>(entity);
            
        // Converte a posição atual do nó Godot para o nosso tipo de domínio
        var currentPosition = bodyRef.Value.GlobalPosition;
        var startPosition = new WorldPosition(
            currentPosition.X,
            currentPosition.Y);

        // Calcula a duração com base na distância e velocidade
        var distance = startPosition.DistanceTo(targetVisualPos);
        var duration = speed.Value > 0 && distance > 0.1f ? distance / speed.Value : 0f;

        // Adiciona/atualiza o componente de tween
        ref var tween = ref World.AddOrGet<MovementTweenComponent>(entity);
        tween.StartPosition = startPosition;
        tween.TargetPosition = targetVisualPos;
        tween.Duration = duration;
        tween.TimeElapsed = 0f;
        
        World.Remove<MovementUpdateCommand>(entity);
    }
}