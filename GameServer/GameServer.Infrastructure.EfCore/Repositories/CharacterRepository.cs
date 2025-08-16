using Game.Core.Entities.Character;
using GameServer.Infrastructure.EfCore.DbContexts;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Repositories;

public sealed class CharacterRepository(
    IDbContextFactory<GameDbContext> dbFactory,
    ILogger<CharacterRepository> logger) : ICharacterRepository
{
    public async Task<CharacterLoadModel?> LoadCharacterAsync(int characterId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, ct)
            .ConfigureAwait(false);
        if (entity == null) return null;

        return new CharacterLoadModel
        {
            CharacterId = entity.Id,
            AccountId = entity.AccountId,
            Name = entity.Name,
            Vocation = entity.Vocation,
            Gender = entity.Gender,
            Direction = entity.Direction,
            Position = entity.Position,
            Speed = entity.Speed,
        };
    }

    public async Task SaveCharactersBatchAsync(IEnumerable<CharacterSaveModel> batch, CancellationToken ct = default)
    {
        // convert to list to enumerate multiple times
        var list = batch.ToList();
        if (list.Count == 0) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Recomendo transação por batch para atomicidade
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            // Buscar registros existentes em um único roundtrip
            var ids = list.Select(x => x.CharacterId).ToList();
            var existing = await db.Characters
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var existingMap = existing.ToDictionary(e => e.Id);

            foreach (var model in list)
            {
                if (existingMap.TryGetValue(model.CharacterId, out var entity))
                {
                    // Atualizar entidade => mapear campos do model para entity
                    MapSaveModelToEntity(model, entity);
                    db.Update(entity); // opcional, EF já rastreia alterações
                }
                else
                {
                    // Inserir novo
                    var newEntity = MapSaveModelToNewEntity(model);
                    await db.AddAsync(newEntity, ct).ConfigureAwait(false);
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar batch de players");
            try { await tx.RollbackAsync(ct).ConfigureAwait(false); } catch { /* swallow */ }
            throw;
        }
    }

    // ValidateLoginAsync foi movido para IAccountRepository.

    // Map helpers: adapte para sua modelagem EF
    private static void MapSaveModelToEntity(CharacterSaveModel model, CharacterEntity entity)
    {
        // Mapear campos do model para a entidade existente
        entity.AccountId = model.AccountId;
        entity.Name = model.Name;
        entity.Vocation = model.Vocation;
        entity.Gender = model.Gender;
        entity.Direction = model.Direction;
        entity.Position = model.Position;
        entity.Speed = model.Speed;
        // Mapear outros campos conforme necessário
    }

    private static CharacterEntity MapSaveModelToNewEntity(CharacterSaveModel model)
    {
        var e = new CharacterEntity
        {
            AccountId = model.AccountId,
            Name = model.Name,
            Vocation = model.Vocation,
            Gender = model.Gender,
            Direction = model.Direction,
            Position = model.Position,
            Speed = model.Speed,
            // mapear campos iniciais
        };
        return e;
    }
}