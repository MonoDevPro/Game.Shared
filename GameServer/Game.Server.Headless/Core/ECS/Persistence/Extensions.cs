using Arch.Core;
using Game.Server.Headless.Core.ECS.Persistence.Components;

namespace Game.Server.Headless.Core.ECS.Persistence;

public static class WorldDirtyExtensions
{
    /// <summary>
    /// Marca a entidade como dirty (adiciona o componente se necessário).
    /// Uso: world.MarkDirty(entity);
    /// </summary>
    public static void MarkDirty(this World world, Entity entity)
    {
        if (!world.Has<DirtyComponent>(entity))
        {
            // Algumas APIs do Arch usam World.Add<T>(entity)
            // Se sua API divergir, ajuste aqui.
            world.Add<DirtyComponent>(entity);
        }
    }

    /// <summary>
    /// Remove a marca de dirty.
    /// </summary>
    public static void ClearDirty(this World world, Entity entity)
    {
        if (world.Has<DirtyComponent>(entity))
            world.Remove<DirtyComponent>(entity);
    }

    public static bool IsDirty(this World world, Entity entity) => world.Has<DirtyComponent>(entity);
}