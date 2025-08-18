using Game.Core.Common.Enums;

namespace Game.Server.Headless.Core.ECS.Game.Components;

public struct DespawnRequest
{
    public int PeerId;
    public int CharacterId;
}