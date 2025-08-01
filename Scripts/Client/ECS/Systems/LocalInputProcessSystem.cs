using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Godot;

namespace Game.Shared.Scripts.Client.ECS.Systems;

public partial class LocalInputProcessSystem(
    World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag, InputComponent>]
    [None<InputRequestCommand>]
    private void UpdateInputCommand(in Entity entity)
    {
        // Get input from Godot's InputMap
        Vector2 input = GodotInputMap.GetMovementInput();
        
        if (input.IsZeroApprox()) // Se o input for zero, não faz nada
            return;
        
        if (input.Length() > 1f) // Se o input for muito alto, normaliza
            input = input.Normalized();
        
        // Pega o componente de input real
        World.Add(entity, new InputRequestCommand{ Value = input });
    }
}
