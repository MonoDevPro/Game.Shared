using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components;
using Game.Core.Entities.Common.Constants;
using Game.Core.Entities.Common.Helpers;
using Game.Core.Entities.Common.ValueObjetcs;
using GameClient.Features.Game.Player.Components;

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
        ref DirectionComponent dir, ref MapPositionComponent gridPos, in SpeedComponent speed,
        in RemoteMoveIntentCommand remoteIntent)
    {
        // Para jogadores remotos, nós confiamos cegamente na posição que o servidor envia.
        // A reconciliação não é necessária, apenas atualizamos a posição base e iniciamos o movimento.
        gridPos.Value = remoteIntent.GridPosition;
        MapPosition targetGridPos = gridPos.Value + remoteIntent.Direction;

        dir.Value = remoteIntent.Direction.ToDirection();

        var startPixelPos = gridPos.Value.ToWorldPosition();
        var targetPixelPos = targetGridPos.ToWorldPosition();
        var pixelDistance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? pixelDistance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementProgressComponent
        {
            StartPosition = gridPos.Value,
            TargetPosition = targetGridPos,
            Duration = duration,
            TimeElapsed = 0f
        });

        World.Remove<RemoteMoveIntentCommand>(entity);
    }
}