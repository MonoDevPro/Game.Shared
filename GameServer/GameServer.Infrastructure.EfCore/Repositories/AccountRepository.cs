using Game.Core.Entities.Account;
using GameServer.Infrastructure.EfCore.DbContexts;
using GameServer.Infrastructure.EfCore.Hasher;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Repositories;

public sealed class AccountRepository(
    IDbContextFactory<GameDbContext> dbFactory,
    IPasswordHasherService hasher,
    ILogger<AccountRepository>? logger = null)
    : IAccountRepository
{
    public async Task<AccountEntity?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username, ct).ConfigureAwait(false);
    }

    public async Task<AccountEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Accounts.AsNoTracking().AnyAsync(a => a.Username == username, ct).ConfigureAwait(false);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email, ct).ConfigureAwait(false);
    }

    public async Task<(bool success, int accountId, string? error)> CreateAccountAsync(string username, string email, string passwordPlaintext, CancellationToken ct = default)
    {
        // normalizar/trim
        username = (username ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();

        // hashed password no servidor
        var hashed = hasher.HashPassword(passwordPlaintext);

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Tentar inserir direto e confiar na constraint unique do DB para concorrência segura.
        var acc = new AccountEntity(username, email, hashed);
        try
        {
            await db.Accounts.AddAsync(acc, ct).ConfigureAwait(false);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return (true, acc.Id, null);
        }
        catch (DbUpdateException dbEx)
        {
            logger?.LogWarning(dbEx, "CreateAccountAsync: possível conflito de unicidade para username/email {Username}/{Email}", username, email);

            // checar especificamente qual foi a violação (melhor analisar InnerException/SqlException)
            if (await db.Accounts.AsNoTracking().AnyAsync(a => a.Username == username, ct).ConfigureAwait(false))
                return (false, 0, "Username already exists");
            if (await db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email, ct).ConfigureAwait(false))
                return (false, 0, "Email already exists");

            return (false, 0, "Database error creating account");
        }
    }

    /// <summary>
    /// Valida credenciais (recebe senha em plaintext) e retorna o personagem associado (se houver) mapeado para DTO.
    /// </summary>
    public async Task<(bool success, int? accountId, CharacterLoadModel? character, string? error)> ValidateLoginAsync(
        string username,
        string passwordPlaintext,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username, ct).ConfigureAwait(false);
        if (account is null)
            return (false, null, null, "Conta não encontrada");

        // comparar plaintext com o hash armazenado
        if (!hasher.VerifyPassword(passwordPlaintext, account.PasswordHash))
            return (false, null, null, "Senha inválida");

        var character = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.AccountId == account.Id, ct).ConfigureAwait(false);
        if (character is null) return (true, account.Id, null, null);

        var dto = new CharacterLoadModel
        {
            CharacterId = character.Id,
            AccountId = character.AccountId,
            Name = character.Name,
            Vocation = character.Vocation,
            Gender = character.Gender,
            Direction = character.Direction,
            Position = character.Position,
            Speed = character.Speed,
        };
        return (true, account.Id, dto, null);
    }
}
