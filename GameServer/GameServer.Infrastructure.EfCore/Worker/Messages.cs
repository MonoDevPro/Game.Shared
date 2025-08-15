using GameServer.Infrastructure.EfCore.Worker.Models;

namespace GameServer.Infrastructure.EfCore.Worker;

// DTOs / messages (pouco duplicado com Messages.cs — mantenha um lugar só no seu projeto)
public sealed record SaveRequest(Guid CommandId, int CharacterId, CharacterSaveModel Data, DateTime EnqueuedUtc);
public sealed record SaveResult(Guid CommandId, int CharacterId, bool Success, string? ErrorMessage);
public sealed record LoginRequest(Guid CommandId, string Username, string PasswordHash, int SenderPeer);
public sealed record LoginResult(Guid CommandId, int SenderPeer, bool Success, CharacterLoadModel? Character, string? ErrorMessage);