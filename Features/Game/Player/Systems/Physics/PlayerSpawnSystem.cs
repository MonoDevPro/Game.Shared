using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.ECS.Components;
using GameClient.Features.Game.Player.Character;
using GameClient.Features.Game.Player.Components;
using GameClient.Features.Game.Player.Events;
using Godot;
using Shared.Features.Player.Components;
using Shared.Features.Player.Components.Tags;

namespace GameClient.Features.Game.Player.Systems.Physics;

public partial class PlayerSpawnSystem(World world, Node sceneRoot) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag>]
    [None<CharNodeRefComponent>]
    private void SpawnViews(in Entity entity, 
        in PlayerInfoComponent playerInfo, 
        in GridPositionComponent gridPosition, 
        in DirectionComponent direction,
        in SpeedComponent speed)
    {
        var characterInitData = new PlayerCharacter.InitializationData(
            playerInfo, gridPosition, direction, speed);
        
        var characterNode = PlayerCharacter.Create(characterInitData);
        var spriteNode = characterNode.CharacterSprite;
        
        // 2. Adicionamos a referência do nó de volta à entidade.
        World.Add(entity, new CharNodeRefComponent{ Value = characterNode });
        World.Add(entity, new CharSpriteRefComponent{ Value = spriteNode });
        
        // 3. Adicionamos o nó à cena.
        sceneRoot.AddChild(characterNode);
        
        // Carrega outras coisas da UI se for o jogador local
        if (World.Has<PlayerControllerTag>(entity))
            EventBus.Send(new LocalPlayerSpawnedEvent { });
    }

    [Query]
    [All<CharNodeRefComponent>]
    [None<NetworkedTag>]
    private void DespawnViews(in Entity entity, ref CharNodeRefComponent nodeRef)
    {
        nodeRef.Value.QueueFree();
        World.Remove<CharNodeRefComponent>(entity);
        
        World.Destroy(entity);
        
        // Descarrega outras coisas da UI se for o jogador local
        if (World.Has<PlayerControllerTag>(entity))
            EventBus.Send(new LocalPlayerDespawnedEvent { });
    }
}