using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.Network.Data.Input;

public struct AttackRequest
{
    public uint SequenceId;
    public GridVector Direction; // Alterado para Vector2I
}