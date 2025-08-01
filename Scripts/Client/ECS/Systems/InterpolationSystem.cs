using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Godot;

namespace Game.Shared.Scripts.Client.ECS.Systems;

/// <summary>
/// Sistema exclusivo do cliente que realiza a interpolação de movimento para entidades remotas.
/// Ele move suavemente os nós de outros jogadores de sua posição anterior para a mais recente
/// recebida do servidor, evitando o movimento "travado" que ocorreria ao aplicar as
/// posições diretamente.
/// </summary>
public partial class InterpolationSystem(World world) : BaseSystem<World, float>(world)
{
    // O tempo total para completar a interpolação.
    // Deve ser um pouco maior que o intervalo de envio do servidor (1/20s = 0.05s) para absorver jitter da rede.
    private const float InterpolationTime = 0.1f; 

    [Query]
    // A query roda em toda entidade remota que tenha dados de interpolação e um corpo físico para mover.
    [All<RemoteProxyTag, InterpolationDataComponent, PositionComponent, SceneBodyRefComponent>]
    private void InterpolateRemotePlayer(
        [Data] in float delta,
        ref InterpolationDataComponent interp,
        ref PositionComponent pos,
        ref SceneBodyRefComponent body)
    {
        // Avança o tempo decorrido da interpolação atual
        interp.TimeElapsed += delta;

        // Calcula o fator de interpolação (alpha), de 0.0 a 1.0.
        float alpha = Mathf.Clamp(interp.TimeElapsed / InterpolationTime, 0.0f, 1.0f);
            
        // Calcula a nova posição usando Interpolação Linear (Lerp).
        Vector2 newPosition = interp.StartPosition.Lerp(interp.TargetPosition, alpha);

        // Aplica a posição interpolada ao nó do Godot e ao componente do ECS.
        body.Value.GlobalPosition = newPosition;
        pos.Value = newPosition;
    }
}
