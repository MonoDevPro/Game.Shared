using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.ECS.Components;
using GameClient.Core.ECS.Components.Tags;
using GameClient.Core.ECS.Components.Visual;
using GameClient.Features.Game.Player.Character;
using Godot;

namespace GameClient.Core.ECS.Systems.Visual;

public partial class PlayerSpawnSystem(World world, Node sceneRoot) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag>]
    [None<CharNodeRefComponent>]
    private void SpawnViews(in Entity entity, 
        in CharInfoComponent playerInfo, 
        in MapPositionComponent gridPosition, 
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
    }

    [Query]
    [All<CharNodeRefComponent>]
    [None<NetworkedTag>]
    private void DespawnViews(in Entity entity, ref CharNodeRefComponent nodeRef)
    {
        nodeRef.Value.QueueFree();
        World.Remove<CharNodeRefComponent>(entity);
        
        World.Destroy(entity);
    }
}