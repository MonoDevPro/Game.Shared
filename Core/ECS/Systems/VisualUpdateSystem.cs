using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.Common;
using GameClient.Core.ECS.Components;
using Godot;
using Shared.Features.Game.Character.Components;

namespace GameClient.Core.ECS.Systems;

/// <summary>
/// Sistema exclusivo do cliente, responsável por traduzir o estado de movimento
/// (MovementStateComponent) em uma interpolação visual suave (tween) para o nó Godot.
/// </summary>
public partial class VisualUpdateSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<MovementProgressComponent, CharNodeRefComponent>]
    private void InterpolateVisuals(ref MovementProgressComponent moveState, ref CharNodeRefComponent charNodeRef)
    {
        // 1. Calcula o fator de interpolação (alpha), garantindo que fique entre 0 e 1.
        float alpha = Mathf.Clamp(moveState.TimeElapsed / moveState.Duration, 0f, 1f);

        // 2. Interpola a posição entre o ponto inicial e o final usando o fator alpha.
        var newPosition = moveState.StartPosition.Lerp(moveState.TargetPosition, alpha);

        // 3. Atualiza a posição visual do nó na cena, convertendo do tipo do domínio para o tipo da Godot.
        charNodeRef.Value.GlobalPosition = newPosition.ToGodotVector2();
    }
}