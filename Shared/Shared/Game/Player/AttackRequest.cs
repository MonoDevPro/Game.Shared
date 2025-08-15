using Game.Core.Entities.Common.ValueObjetcs;

namespace Shared.Game.Player;

public struct AttackRequest
{
    public uint SequenceId;
    public MapPosition Direction; // Alterado para Vector2I
}