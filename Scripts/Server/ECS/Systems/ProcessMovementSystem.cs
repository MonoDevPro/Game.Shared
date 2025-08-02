using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using Godot;

namespace Game.Shared.Scripts.Server.ECS.Systems;

public partial class ProcessMovementSystem(World world, PlayerSpawner spawner) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;

    [Query]
    [All<NetworkedTag, GridPositionComponent, MoveIntentCommand>]
    private void ProcessMove(in Entity entity, in NetworkedTag netTag, ref GridPositionComponent gridPos, in MoveIntentCommand intent)
    {
        Vector2I targetGridPos = gridPos.Value + intent.Direction;

        // --- LÓGICA DE VALIDAÇÃO (Exemplo) ---
        // Aqui você verificaria se o tile `targetGridPos` é caminhável.
        // bool canMove = IsTileWalkable(targetGridPos);
        bool canMove = true; // Por enquanto, qualquer movimento é válido.

        if (canMove)
        {
            gridPos.Value = targetGridPos;

            // Notifica todos os clientes sobre a nova posição.
            var packet = new StateResponse
            {
                NetId = netTag.Id,
                GridPosition = gridPos.Value
            };
            spawner.NetworkManager.Sender.Broadcast(ref packet);
        }

        // Remove o comando e a tag de movimento, permitindo um novo input.
        World.Remove<MoveIntentCommand>(entity);
        World.Remove<IsMovingTag>(entity); // O servidor controla quando o jogador pode se mover novamente.
    }
}