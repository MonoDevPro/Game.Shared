using Shared.Infrastructure.Math;

namespace Shared.Core.Network.Data.Input;

public struct AttackRequest
{
    public uint SequenceId;
    public GridVector Direction; // Alterado para Vector2I
}