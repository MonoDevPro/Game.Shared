using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Godot;

namespace Game.Shared.Shared.Infrastructure.ECS.Systems;

/// <summary>
/// Sistema compartilhado que move fisicamente uma entidade em direção a um alvo.
/// Roda tanto no cliente (predição) quanto no servidor (autoritativo).
/// </summary>
public partial class ProcessMovementSystem : BaseSystem<World, float>
{
    private const int GridSize = 32;

    public ProcessMovementSystem(World world) : base(world) { }

    /// <summary>
    /// Query 1: Inicia o movimento a partir de uma intenção.
    /// Converte a intenção de movimento em um alvo (TargetPositionComponent).
    /// </summary>
    [Query]
    [All<MoveIntentCommand>]
    [None<IsMovingTag>] // Garante que não vamos processar um novo movimento enquanto um já ocorre.
    private void InitiateMovement(in Entity entity, ref GridPositionComponent gridPos, in MoveIntentCommand cmd)
    {
        // Calcula a posição do grid e dos pixels de destino.
        var targetGridPos = gridPos.Value + cmd.Direction;
        var targetPixelPos = new Vector2(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
        
        // Adiciona os componentes que marcam o início do estado de movimento.
        World.Add(entity, new TargetPositionComponent { Value = targetPixelPos });
        World.Add<IsMovingTag>(entity); // Bloqueia novos inputs.
            
        // Remove o comando, pois a intenção já foi processada.
        World.Remove<MoveIntentCommand>(entity);
    }
    
    /// <summary>
    /// Query 2: Executa o movimento contínuo a cada frame de física.
    /// Move a entidade em direção ao alvo e verifica se o movimento terminou.
    /// </summary>
    [Query]
    [All<IsMovingTag, SceneBodyRefComponent, SpeedComponent, TargetPositionComponent>]
    private void ExecuteMovement(
        in Entity entity, 
        ref SceneBodyRefComponent bodyRef, 
        in SpeedComponent speed, 
        in TargetPositionComponent target)
    {
        var characterBody = bodyRef.Value;
        var targetPosition = target.Value;

        // Se a distância para o alvo for muito pequena, consideramos que o movimento terminou.
        if (characterBody.GlobalPosition.DistanceTo(targetPosition) < 1.0f)
        {
            // Finaliza o movimento
            characterBody.Velocity = Vector2.Zero;
            characterBody.GlobalPosition = targetPosition; // Garante a posição final exata.

            // Atualiza a posição lógica do grid.
            ref var gridPos = ref World.Get<GridPositionComponent>(entity);
            gridPos.Value = new Vector2I((int)(targetPosition.X / GridSize), (int)(targetPosition.Y / GridSize));

            // Limpa os componentes de estado de movimento.
            World.Remove<TargetPositionComponent>(entity);
            World.Remove<IsMovingTag>(entity); // Libera para o próximo movimento.
        }
        else
        {
            // Continua o movimento
            var direction = characterBody.GlobalPosition.DirectionTo(targetPosition);
            characterBody.Velocity = direction * speed.Value;
            characterBody.MoveAndSlide();
        }
    }
}