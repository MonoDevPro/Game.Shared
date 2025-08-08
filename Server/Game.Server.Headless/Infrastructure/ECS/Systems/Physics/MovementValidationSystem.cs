using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Math;
using Shared.Infrastructure.Network.Data.Input;
using Shared.Infrastructure.Network.Transport;
using Shared.Infrastructure.World;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Physics;

public partial class MovementValidationSystem(World world, NetworkSender sender, GameMap gameMap, ILogger<MovementValidationSystem> logger) 
    : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;

    [Query]
    [All<NetworkedTag, GridPositionComponent, MoveIntentCommand>]
    private void ProcessMove(in Entity entity, 
        in NetworkedTag netTag, ref GridPositionComponent gridPos, in MoveIntentCommand intent)
    {
        GridVector targetGridPos = gridPos.Value + intent.Direction;

        // --- LÓGICA DE VALIDAÇÃO (Exemplo) ---
        if (gameMap.IsTileWalkable(targetGridPos))
        {
            // Inicia o movimento autoritativo no servidor.
            var targetPixelPos = new WorldPosition(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
            World.Add(entity, new TargetPositionComponent { Value = targetPixelPos });
            
            // Envia a intenção de movimento para os jogadores clientes.
            var packet = new MovementUpdateResponse
            {
                NetId = netTag.Id,
                GridPosition = targetGridPos
            };
            
            // Envia o pacote de movimento para todos os clientes conectados.
            sender.EnqueueReliableBroadcastExcept(netTag.Id, ref packet);
        }
        else
        {
            // Se o movimento for inválido, remove a tag para permitir novo input
            World.Remove<MoveIntentCommand>(entity);
            
            // Opcional: Enviar uma mensagem de erro ou feedback ao cliente
            logger.LogWarning("Movimento inválido para {NetId} na direção {Direction}.", netTag.Id, intent.Direction);
            
            // Envia uma mensagem de erro para o cliente (opcional)
            var updateMovement = new MovementUpdateResponse
            {
                NetId = netTag.Id,
                GridPosition = gridPos.Value // Retorna a posição atual
            };
            
            sender.EnqueueReliableSend(netTag.Id, ref updateMovement);
        }
    }
}