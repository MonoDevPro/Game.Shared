using Game.Core.Common.Enums;

namespace Game.Server.Headless.Core.ECS.Game.Components;

public struct SpawnRequest
{
    public int PeerId;
    public int CharacterId;
    public string Name;
    public VocationEnum Vocation;
    public GenderEnum Gender;
}
