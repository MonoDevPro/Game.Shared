using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Common.Helpers;
using Game.Core.Common.ValueObjetcs;
using Game.Core.Entities.Map;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Intents;
using Game.Server.Headless.Core.ECS.Game.Components.States;
using Game.Server.Headless.Core.ECS.Game.Components.Tags;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.Game.Player;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.Game.Systems;

public partial class MovementSystem(World world, GameMap gameMap, NetworkSender sender, ILogger<MovementSystem> logger)
    : BaseSystem<World, float>(world)
{
    // Parte 1: Inicia o movimento. Válido para Cliente (predição) e Servidor (autoridade).
    [Query]
    [All<NetworkedTag, MoveIntent, MapPositionComponent, SpeedComponent>]
    [None<MovementProgressComponent>]
    private void StartMovement(in Entity entity,
        ref DirectionComponent dir, ref MapPositionComponent mapPos, in SpeedComponent speed,
    in MoveIntent intent)
    {
        MapPosition targetGridPos = mapPos.Value + intent.Direction;

        if (!gameMap.IsTileWalkable(targetGridPos))
        {
            World.Remove<MoveIntent>(entity);
            logger.LogWarning("Movimento inválido na direção {dir} do nó {Entity}.", intent.Direction, entity);
            return;
        }
        // Converte o vetor de movimento (ex: {X:1, Y:0}) para a enumeração (ex: DirectionEnum.East)
        dir.Value = intent.Direction.ToDirection();

        var distance = MovementHelper.CalculateMovementDuration(mapPos.Value, targetGridPos, speed.Value);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementProgressComponent
        {
            StartPosition = mapPos.Value,
            TargetPosition = targetGridPos,
            Duration = duration,
            TimeElapsed = 0f
        });
    }

    [Query]
    [All<NetworkedTag, MoveIntent, MovementProgressComponent>]
    private void SendMovementUpdate(in Entity entity, in NetworkedTag netTag,
        MoveIntent command, in MapPositionComponent gridPos)
    {
        logger.LogDebug("Enviando atualização de movimento para todos exceto: {NetId}, Direção: {Direction}, Posição: {Position}",
            netTag.Id, command.Direction, gridPos.Value);

        // Se for o servidor, envia a atualização de movimento para os clientes.
        // Envia o pacote de movimento para todos os clientes conectados.
        var packet = new MovementStartResponse
        {
            NetId = netTag.Id,
            TargetDirection = command.Direction,
            CurrentPosition = gridPos.Value
        };

        sender.EnqueueReliableBroadcastExcept(netTag.Id, ref packet);

        // Removemos o comando de intenção de movimento, pois já foi processado.
        World.Remove<MoveIntent>(entity);
    }

    // Parte 2: Processa o progresso do movimento.
    [Query]
    [All<MovementProgressComponent>]
    private void ProcessMovement([Data] float delta, in Entity entity, ref MapPositionComponent mapPos, ref MovementProgressComponent moveState)
    {
        moveState.TimeElapsed += delta;

        // Quando o tempo do movimento termina...
        if (moveState.TimeElapsed >= moveState.Duration)
        {
            // ...o estado lógico é atualizado para a posição final.
            // Isso acontece de forma idêntica no cliente e no servidor.
            mapPos.Value = moveState.TargetPosition;

            // O movimento terminou, removemos o componente de estado.
            World.Remove<MovementProgressComponent>(entity);
        }
    }
}