using Shared.Infrastructure.Math;

namespace Shared.Features.Player.Packets.Input;

public struct AttackRequest
{
    public uint SequenceId;
    public GridVector Direction; // Alterado para Vector2I
}