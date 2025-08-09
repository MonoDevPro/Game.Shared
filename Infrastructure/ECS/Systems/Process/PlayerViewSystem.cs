using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.Adapters;
using GameClient.Infrastructure.ECS.Components;
using GameClient.Presentation.Entities.Character;
using Godot;
using Shared.Core.Constants;
using Shared.Infrastructure.ECS.Components;

namespace GameClient.Infrastructure.ECS.Systems.Process;

public partial class PlayerViewSystem(World world, Node sceneRoot) : BaseSystem<World, float>(world)
{
    private const string CharacterScenePath = "res://Presentation/Entities/Character/PlayerCharacter.tscn";
    private readonly PackedScene _characterScene = GD.Load<PackedScene>(CharacterScenePath);

    [Query]
    [All<PlayerInfoComponent, GridPositionComponent>]
    [None<SceneBodyRefComponent>]
    private void SpawnViews(in Entity entity, in PlayerInfoComponent playerInfo, in GridPositionComponent gridPosition)
    {
        var characterNode = _characterScene.Instantiate<PlayerCharacter>();
        
        // 1. Passamos os dados para o nó.
        characterNode.Initialize(playerInfo);
        
        // 2. Adicionamos a referência do nó de volta à entidade.
        World.Add(entity, new SceneBodyRefComponent{ Value = characterNode });
        
        characterNode.GlobalPosition = (gridPosition.Value.ToGodotVector2I() * GameMapConstants.GridSize) + (Vector2.One * GameMapConstants.GridSize / 2);
        
        // 3. Adicionamos o nó à cena.
        sceneRoot.AddChild(characterNode);
    }

    [Query]
    [All<SceneBodyRefComponent>]
    [None<PlayerInfoComponent>]
    private void DespawnViews(in Entity entity, ref SceneBodyRefComponent sceneRef)
    {
        sceneRef.Value.QueueFree();
        World.Remove<SceneBodyRefComponent>(entity);
    }
}