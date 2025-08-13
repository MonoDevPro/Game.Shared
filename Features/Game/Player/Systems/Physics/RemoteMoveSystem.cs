using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Features.Game.Player.Components;
using Shared.Core.Common.Constants;
using Shared.Core.Common.Helpers;
using Shared.Core.Common.Math;
using Shared.Features.Game.Character.Components;

// Importa o RemoteProxyTag

namespace GameClient.Features.Game.Player.Systems.Physics;

/// <summary>
/// Sistema cliente que lida com as atualizações de estado do servidor para jogadores remotos.
/// Inicia a interpolação de movimento (tween) para entidades que são "proxies" de outros jogadores.
/// </summary>
public partial class RemoteMoveSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;
    
    [Query]
    [All<RemoteProxyTag, RemoteMoveIntentCommand>] // <-- MUDANÇA: Agora só atua em proxies remotos
    [None<MovementProgressComponent>]
    private void StartMovement(in Entity entity, 
        ref DirectionComponent dir, ref GridPositionComponent gridPos, in SpeedComponent speed, 
        in RemoteMoveIntentCommand remoteIntent)
    {
        // Para jogadores remotos, nós confiamos cegamente na posição que o servidor envia.
        // A reconciliação não é necessária, apenas atualizamos a posição base e iniciamos o movimento.
        gridPos.Value = remoteIntent.GridPosition;
        GridVector targetGridPos = gridPos.Value + remoteIntent.Direction;

        dir.Value = remoteIntent.Direction.ToDirection();
            
        var startPixelPos = WorldPosition.FromGridPosition(gridPos.Value, GridSize);
        var targetPixelPos = WorldPosition.FromGridPosition(targetGridPos, GridSize);
            
        var distance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementProgressComponent
        {
            StartPosition = startPixelPos,
            TargetPosition = targetPixelPos,
            Duration = duration,
            TimeElapsed = 0f
        });
        
        World.Remove<RemoteMoveIntentCommand>(entity);
    }
}