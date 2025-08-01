// No projeto do Cliente: Game.Client.Scripts.ECS.Systems.ReconciliationSystem.cs

using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Godot;

namespace Game.Shared.Scripts.Client.ECS.Systems;

/// <summary>
/// Sistema exclusivo do cliente responsável pela Reconciliação de Estado.
/// Ele compara o estado autoritativo recebido do servidor com o estado
/// previsto localmente para a entidade do jogador. Se a discrepância
/// (erro de predição) for muito grande, ele corrige forçadamente a posição
/// do jogador para sincronizá-lo com o servidor.
/// </summary>
public partial class ReconciliationSystem(World world) : BaseSystem<World, float>(world)
{
    // Define a distância máxima que a predição pode errar antes de forçar uma correção.
    // Um valor pequeno evita correções por imprecisões de ponto flutuante.
    private const float ReconciliationThreshold = 0.05f; // 5 pixels de erro máximo

    [Query]
    // A query roda em qualquer entidade que seja do jogador local E tenha um comando de estado do servidor para processar.
    [All<PlayerControllerTag, PositionComponent, SceneBodyRefComponent, AuthoritativeStateCommand>]
    private void ReconcilePlayerState(
        in Entity entity, 
        ref PositionComponent localPos, 
        ref SceneBodyRefComponent body, 
        in AuthoritativeStateCommand serverState)
    {
        float errorDistance = localPos.Value.DistanceTo(serverState.Position);

        if (errorDistance > ReconciliationThreshold)
        {
            GD.Print($"[RECONCILIATION] Erro de predição detectado! Distância: {errorDistance}. Corrigindo posição.");

            // Confia no servidor e aplica o estado autoritativo.
            localPos.Value = serverState.Position;
            body.Value.GlobalPosition = serverState.Position;
                
            // Também é importante corrigir a velocidade, pois ela afeta o próximo frame de predição.
            ref var localVel = ref World.Get<VelocityComponent>(entity);
            localVel.Value = serverState.Velocity;
        }

        // IMPORTANTE: Remove o comando após processá-lo para não reconciliar novamente.
        World.Remove<AuthoritativeStateCommand>(entity);
    }
}
