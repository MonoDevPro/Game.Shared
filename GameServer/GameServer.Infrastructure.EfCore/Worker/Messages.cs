using Game.Core.Entities.Common.Enums;
using GameServer.Infrastructure.EfCore.Worker.Models;

namespace GameServer.Infrastructure.EfCore.Worker;

// DTOs / messages (pouco duplicado com Messages.cs — mantenha um lugar só no seu projeto)
public sealed record SaveRequest(Guid CommandId, int CharacterId, CharacterSaveModel Data, DateTime EnqueuedUtc);
public sealed record SaveResult(Guid CommandId, int CharacterId, bool Success, string? ErrorMessage);
public sealed record LoginRequest(Guid CommandId, string Username, string PasswordHash, int SenderPeer);
public sealed record LoginResult(Guid CommandId, int SenderPeer, bool Success, int? AccountId, CharacterLoadModel? Character, string? ErrorMessage);

// Main Menu specific
public sealed record AccountCreationRequestMsg(Guid CommandId, string Username, string Email, string Password, int SenderPeer);
public sealed record AccountCreationResult(Guid CommandId, int SenderPeer, bool Success, string? ErrorMessage);

public sealed record CharacterListRequestMsg(Guid CommandId, int AccountId, int SenderPeer);
public sealed record CharacterListResult(Guid CommandId, int SenderPeer, IReadOnlyList<CharacterSummaryModel> Characters);

public sealed record CharacterCreationRequestMsg(Guid CommandId, int AccountId, string Name, VocationEnum Vocation, GenderEnum Gender, int SenderPeer);
public sealed record CharacterCreationResult(Guid CommandId, int SenderPeer, bool Success, CharacterSummaryModel? Character, string? ErrorMessage);

public sealed record CharacterSelectionRequestMsg(Guid CommandId, int AccountId, int CharacterId, int SenderPeer);
public sealed record CharacterSelectionResult(Guid CommandId, int SenderPeer, bool Success, CharacterSummaryModel? Character, string? ErrorMessage);