using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.Adapters;
using GameClient.Infrastructure.ECS.Components;
using Godot;
using Shared.Infrastructure.ECS.Components;

namespace GameClient.Infrastructure.ECS.Systems.Process;

/// <summary>
/// Sistema exclusivo do cliente, responsável por traduzir o estado de movimento
/// (MovementStateComponent) em uma interpolação visual suave (tween) para o nó Godot.
/// </summary>
public partial class VisualUpdateSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<MovementStateComponent, SceneCharRefComponent>]
    private void InterpolateVisuals(ref MovementStateComponent moveState, ref SceneCharRefComponent charRef)
    {
        // Garante que o nó da cena ainda exista antes de tentar movê-lo.
        /*if (!GodotObject.IsInstanceValid(bodyRef.Value))
            return;*/
        
        GD.Print($"Interpolando visualmente a entidade {charRef.Value.Name} com estado de movimento: {moveState}");

        // 1. Calcula o fator de interpolação (alpha), garantindo que fique entre 0 e 1.
        float alpha = Mathf.Clamp(moveState.TimeElapsed / moveState.Duration, 0f, 1f);

        // 2. Interpola a posição entre o ponto inicial e o final usando o fator alpha.
        var newPosition = moveState.StartPosition.Lerp(moveState.TargetPosition, alpha);

        // 3. Atualiza a posição visual do nó na cena, convertendo do tipo do domínio para o tipo da Godot.
        charRef.Value.GlobalPosition = newPosition.ToGodotVector2();
    }
}