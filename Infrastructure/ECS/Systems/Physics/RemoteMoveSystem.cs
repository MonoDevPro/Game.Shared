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
public partial class RemoteMoveSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag, RemoteMoveCommand>]
    [None<MovementStateComponent>] // Não processa se já houver movimento em andamento
    private void HandleRemoteMove(in Entity entity, 
        in RemoteMoveCommand update, ref GridPositionComponent grid)
    {
        GD.Print($"Recebido RemoteMoveCommand para a entidade {entity} com posição de grade {update.LastGridPosition} e direção {update.DirectionInput}.");
        
        grid.Value = update.LastGridPosition;
        ref var intent = ref World.AddOrGet<MoveIntentCommand>(entity);
        intent.Direction = update.DirectionInput;
        
        World.Remove<RemoteMoveCommand>(entity);
    }
}