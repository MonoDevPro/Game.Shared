using Arch.Core;

namespace Game.Server.Headless.Core.ECS.Persistence;

public struct CommandMetaComponent
{
    public Guid CommandId;
}

public struct DirtyComponent
{
    public DateTime MarkedUtc;
}

public struct SavePendingComponent
{
    public Guid CommandId;
}

public struct CharacterIdComponent
{
    public int CharacterId; // identificação estável do player (CharacterId / PlayerId)
}

public struct LoginRequestComponent
{
    public string Username;
    public string PasswordHash;
}

public struct SenderPeerComponent
{
    public int PeerId;
}