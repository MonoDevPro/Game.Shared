using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Resources.Client;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Enums;
using Godot;

namespace Game.Shared.Scripts.Client.ECS.Systems
{
    /// <summary>
    /// Sistema exclusivo do cliente que lê o estado de movimento da entidade (velocidade)
    /// e atualiza o componente CharacterSprite para exibir a animação correta.
    /// </summary>
    public partial class AnimationSystem : BaseSystem<World, float>
    {
        public AnimationSystem(World world) : base(world) { }

        [Query]
        [All<VelocityComponent, SceneBodyRefComponent>]
        private void UpdateAnimations(ref SceneBodyRefComponent body, in VelocityComponent vel)
        {
            // O CharacterSprite é um filho do CharacterScript (que é o SceneBodyRefComponent.Value)
            var sprite = body.Value.GetNodeOrNull<CharacterSprite>("CharacterSprite");
            if (sprite == null) return;

            var action = ActionEnum.Idle;
            var direction = sprite.Direction; // Mantém a direção anterior se parado

            // Se o personagem está se movendo
            if (vel.Value.LengthSquared() > 0.1f)
            {
                action = ActionEnum.Walk;
                direction = VectorToDirection(vel.Value);
            }

            // Define o estado no CharacterSprite, que cuidará de tocar a animação correta
            sprite.SetState(action, direction);
        }

        private DirectionEnum VectorToDirection(Vector2 velocity)
        {
            if (velocity.LengthSquared() == 0) return DirectionEnum.None;

            var angle = Mathf.RadToDeg(velocity.Angle());

            if (angle < 0) angle += 360;

            if (angle >= 337.5 || angle < 22.5) return DirectionEnum.East;
            if (angle >= 22.5 && angle < 67.5) return DirectionEnum.SouthEast;
            if (angle >= 67.5 && angle < 112.5) return DirectionEnum.South;
            if (angle >= 112.5 && angle < 157.5) return DirectionEnum.SouthWest;
            if (angle >= 157.5 && angle < 202.5) return DirectionEnum.West;
            if (angle >= 202.5 && angle < 247.5) return DirectionEnum.NorthWest;
            if (angle >= 247.5 && angle < 292.5) return DirectionEnum.North;
            if (angle >= 292.5 && angle < 337.5) return DirectionEnum.NorthEast;

            return DirectionEnum.None;
        }
    }
}