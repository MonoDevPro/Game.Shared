using Godot;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;

// Usa os novos tipos de matemática
namespace GameClient.Infrastructure.ECS.Systems.Physics;

/// <summary>
/// Sistema cliente que lida com as atualizações de estado do servidor.
/// - Para jogadores remotos, inicia a interpolação de movimento (tween).
/// - Para o jogador local, corrige a posição se houver dessincronização.
/// </summary>
public partial class MovementUpdateSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag, MovementUpdateCommand>]
    private void HandleMovementUpdate(in Entity entity, 
        in MovementUpdateCommand update, ref GridPositionComponent grid)
    {
        GD.Print($"Recebido MovementUpdateCommand para a entidade {entity} com posição de grade {update.LastGridPosition} e direção {update.DirectionInput}.");
        
        grid.Value = update.LastGridPosition;
        
        if (World.Has<MovementStateComponent>(entity))
            World.Remove<MovementStateComponent>(entity);
        
        ref var intent = ref World.AddOrGet<MoveIntentCommand>(entity);
        intent.Direction = update.DirectionInput;
        World.Remove<MovementUpdateCommand>(entity);
    }
}