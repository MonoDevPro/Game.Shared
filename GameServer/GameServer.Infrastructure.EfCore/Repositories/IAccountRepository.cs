using Game.Core.Entities.Account;
using GameServer.Infrastructure.EfCore.Worker.Models;

namespace GameServer.Infrastructure.EfCore.Repositories;

public interface IAccountRepository
{
    Task<AccountEntity?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AccountEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<(bool success, int accountId, string? error)> CreateAccountAsync(string username, string email, string passwordPlaintext, CancellationToken ct = default);

    /// <summary>
    /// Valida credenciais e retorna o personagem associado (se houver) já mapeado para DTO.
    /// </summary>
    Task<(bool success, int? accountId, CharacterLoadModel? character, string? error)> ValidateLoginAsync(
        string username,
        string passwordHash,
        CancellationToken ct = default);
}
