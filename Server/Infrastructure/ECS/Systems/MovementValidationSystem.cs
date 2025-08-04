using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Input;
using Game.Shared.Shared.Infrastructure.Spawners;
using Godot;

namespace Game.Shared.Server.Infrastructure.ECS.Systems;

/// <summary>
/// No servidor, valida a intenção de movimento recebida e, se for válida,
/// inicia o movimento autoritativo.
/// </summary>
public partial class MovementValidationSystem(World world, PlayerSpawner spawner) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;

    [Query]
    [All<NetworkedTag, GridPositionComponent, MoveIntentCommand>]
    private void ProcessMove(in Entity entity, in NetworkedTag netTag, ref GridPositionComponent gridPos, in MoveIntentCommand intent)
    {
        Vector2I targetGridPos = gridPos.Value + intent.Direction;

        // --- LÓGICA DE VALIDAÇÃO (Exemplo) ---
        bool canMove = true; // Aqui você verificaria colisões, etc.

        if (canMove)
        {
            // Inicia o movimento autoritativo no servidor.
            var targetPixelPos = new Vector2(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
            World.Add(entity, new TargetPositionComponent { Value = targetPixelPos });
            
            // Envia a intenção de movimento para os jogadores clientes.
            var packet = new MovementUpdateResponse
            {
                NetId = netTag.Id,
                GridPosition = targetGridPos
            };
            
            // Envia o pacote de movimento para todos os clientes conectados.
            spawner.NetworkManager.Sender.EnqueueReliableBroadcast(ref packet);
        }
        else
        {
            // Se o movimento for inválido, remove a tag para permitir novo input
            World.Remove<MoveIntentCommand>(entity);
            
            // Opcional: Enviar uma mensagem de erro ou feedback ao cliente
            GD.Print($"Movimento inválido para {netTag.Id} na direção {intent.Direction}.");
            
            // Envia uma mensagem de erro para o cliente (opcional)
            var updateMovement = new MovementUpdateResponse
            {
                NetId = netTag.Id,
                GridPosition = gridPos.Value // Retorna a posição atual
            };
            
            spawner.NetworkManager.Sender.EnqueueReliableSend(netTag.Id, ref updateMovement);
        }
    }
}