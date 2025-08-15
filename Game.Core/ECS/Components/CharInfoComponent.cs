using Game.Core.Entities.Common.Enums;

namespace Game.Core.ECS.Components;

public struct CharInfoComponent
{
    public string Name;
    public VocationEnum Vocation;
    public GenderEnum Gender;
}