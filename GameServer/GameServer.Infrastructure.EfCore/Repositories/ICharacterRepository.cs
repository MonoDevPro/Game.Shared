using GameServer.Infrastructure.EfCore.Worker.Models;

namespace GameServer.Infrastructure.EfCore.Repositories;

public interface ICharacterRepository
{
    Task<CharacterLoadModel?> LoadCharacterAsync(int characterId, CancellationToken ct = default);
    Task SaveCharactersBatchAsync(IEnumerable<CharacterSaveModel> batch, CancellationToken ct = default);
    Task<(bool success, CharacterLoadModel? character, string? error)> ValidateLoginAsync(string username, string passwordHash, CancellationToken ct = default);
}