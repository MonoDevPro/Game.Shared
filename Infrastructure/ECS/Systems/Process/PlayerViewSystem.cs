using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.Adapters;
using GameClient.Infrastructure.ECS.Components;
using GameClient.Presentation.Entities.Character;
using GameClient.Presentation.Entities.Character.Sprites;
using Godot;
using Shared.Core.Constants;
using Shared.Infrastructure.ECS.Components;

namespace GameClient.Infrastructure.ECS.Systems.Process;

public partial class PlayerViewSystem(World world, Node sceneRoot) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerInfoComponent>]
    [None<SceneCharRefComponent>]
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
        World.Add(entity, new SceneCharRefComponent{ Value = characterNode });
        World.Add(entity, new SpriteRefComponent{ Value = spriteNode });
        
        // 3. Adicionamos o nó à cena.
        sceneRoot.AddChild(characterNode);
    }

    [Query]
    [All<SceneCharRefComponent>]
    [None<PlayerInfoComponent>]
    private void DespawnViews(in Entity entity, ref SceneCharRefComponent sceneRef)
    {
        sceneRef.Value.QueueFree();
        World.Remove<SceneCharRefComponent>(entity);
    }
}