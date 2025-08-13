using Shared.Core.Common.Math;

namespace Shared.Features.Game.Character.Packets.Input;

public struct AttackRequest
{
    public uint SequenceId;
    public GridVector Direction; // Alterado para Vector2I
}