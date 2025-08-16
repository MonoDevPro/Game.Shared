using Game.Core.Entities.Account;
using Game.Core.Entities.Character;
using GameServer.Infrastructure.EfCore.DbContexts;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Repositories;

public sealed class AccountRepository(
    IDbContextFactory<GameDbContext> dbFactory,
    GameServer.Infrastructure.EfCore.Hasher.IPasswordHasherService hasher) : IAccountRepository
{
    public async Task<AccountEntity?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username, ct);
    }

    public async Task<AccountEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().AnyAsync(a => a.Username == username, ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email, ct);
    }

    public async Task<(bool success, int accountId, string? error)> CreateAccountAsync(string username, string email, string passwordPlaintext, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Accounts.AnyAsync(a => a.Username == username, ct))
            return (false, 0, "Username already exists");
        if (await db.Accounts.AnyAsync(a => a.Email == email, ct))
            return (false, 0, "Email already exists");

        var hashed = hasher.HashPassword(passwordPlaintext);
        var acc = new AccountEntity(username, email, hashed);
        await db.Accounts.AddAsync(acc, ct);
        await db.SaveChangesAsync(ct);
        return (true, acc.Id, null);
    }

    public async Task<(bool success, int? accountId, CharacterLoadModel? character, string? error)> ValidateLoginAsync(string username, string passwordHash, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username, ct);
        if (account is null)
            return (false, null, null, "Conta não encontrada");

        if (!hasher.VerifyPassword(passwordHash, account.PasswordHash))
            return (false, null, null, "Senha inválida");

        var character = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.AccountId == account.Id, ct);
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
