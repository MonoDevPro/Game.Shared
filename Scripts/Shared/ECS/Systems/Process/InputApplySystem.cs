using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Godot;

namespace Game.Shared.Scripts.Shared.ECS.Systems.Process;

/// <summary>
/// Este sistema "aplica" o estado do InputComponent para gerar uma VelocityComponent.
/// Ele também zera o InputComponent para que o input não seja reaplicado continuamente.
/// Roda tanto no cliente quanto no servidor.
/// </summary>
// TODO: [UpdateInGroup(typeof(SimulationSystemGroup))]
// TODO: [UpdateAfter(typeof(InputRequestSystem))] // Ativado!
public partial class InputApplySystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<InputComponent>]
    private void ApplyInputToVelocity(ref InputComponent input, ref VelocityComponent vel, in SpeedComponent sp)
    {
        // Aplica o Input na velocidade, normalizado para evitar movimento mais rápido na diagonal
        vel.Value = input.Value.Normalized() * sp.Value;
            
        // Reseta o componente de Input após aplicar para o estado do próximo frame
        input.Value = Vector2.Zero;
    }
}
