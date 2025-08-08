using Shared.Core.Enums;

namespace Shared.Infrastructure.ECS.Components;

public struct PlayerInfoComponent
{
    public string Name;
    public VocationEnum Vocation;
    public GenderEnum Gender;
}