using Arch.Core;

namespace Game.Server.Headless.Core.ECS.Game.Components;

/// <summary>
/// Um componente singleton que atua como um registro central para
/// mapear Network IDs para Entidades de jogadores.
/// </summary>
public struct PlayerRegistryComponent
{
    public Dictionary<int, Entity> PlayersByNetId;
}