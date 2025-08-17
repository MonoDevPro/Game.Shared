using Game.Core.Entities.Character;
using GameServer.Infrastructure.EfCore.DbContexts;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Repositories;

public sealed class CharacterRepository(IDbContextFactory<GameDbContext> dbFactory, ILogger<CharacterRepository> logger)
    : ICharacterRepository
{
    /// <summary>
    /// Carrega o personagem e mapeia para DTO. Inclui relacionamentos se necessário.
    /// </summary>
    public async Task<CharacterLoadModel?> LoadCharacterAsync(int characterId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Exemplo com include de coleções/entidades relacionadas — ajuste nomes conforme seu modelo
        var query = db.Characters
            .AsNoTracking()
            //.Include(c => c.Inventory).ThenInclude(i => i.Items) // descomente se precisar do inventário
            //.Include(c => c.Stats) // descomente se houver stats em entidade separada
            .Where(c => c.Id == characterId);

        var entity = await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (entity is null) return null;

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
            // Mapear inventário / stats se precisar
        };
    }

    /// <summary>
    /// Salva um batch de personagens (upsert). Usa transação por batch e mapeamento campo-a-campo.
    /// </summary>
    public async Task SaveCharactersBatchAsync(IEnumerable<CharacterSaveModel> batch, CancellationToken ct = default)
    {
        var list = batch?.ToList() ?? new List<CharacterSaveModel>();
        if (list.Count == 0) return;

        logger.LogDebug("SaveCharactersBatchAsync: salvando {Count} characters", list.Count);

        // dividir em sub-batches se muito grande (proteção)
        const int maxSubBatch = 500;
        var subBatches = (int)Math.Ceiling(list.Count / (double)maxSubBatch);

        for (int i = 0; i < subBatches; i++)
        {
            var sub = list.Skip(i * maxSubBatch).Take(maxSubBatch).ToList();
            await SaveCharactersBatchInternalAsync(sub, ct).ConfigureAwait(false);
        }
    }

    private async Task SaveCharactersBatchInternalAsync(List<CharacterSaveModel> list, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Transação por sub-batch
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var ids = list.Select(x => x.CharacterId).ToList();

            // Carregar existentes (incluir coleções se for atualizar coleções)
            var existing = await db.Characters
                //.Include(c => c.Inventory).ThenInclude(i => i.Items) // se for atualizar inventário
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var existingMap = existing.ToDictionary(e => e.Id);

            foreach (var model in list)
            {
                if (existingMap.TryGetValue(model.CharacterId, out var entity))
                {
                    MapSaveModelToEntity(model, entity);
                    // EF já rastreia alterações; Update() é opcional
                }
                else
                {
                    var newEntity = MapSaveModelToNewEntity(model);
                    await db.AddAsync(newEntity, ct).ConfigureAwait(false);
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            logger.LogWarning(concEx, "Concurrency exception ao salvar characters batch (count={Count})", list.Count);
            try { await tx.RollbackAsync(ct).ConfigureAwait(false); } catch { /* swallow */ }
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar batch de characters (count={Count})", list.Count);
            try { await tx.RollbackAsync(ct).ConfigureAwait(false); } catch { /* swallow */ }
            throw;
        }
    }

    // Map helpers: adapte para sua modelagem EF
    private static void MapSaveModelToEntity(CharacterSaveModel model, CharacterEntity entity)
    {
        // Mapear campos do model para a entidade existente (campo-a-campo)
        entity.AccountId = model.AccountId;
        entity.Name = model.Name;
        entity.Vocation = model.Vocation;
        entity.Gender = model.Gender;
        entity.Direction = model.Direction;
        entity.Position = model.Position;
        entity.Speed = model.Speed;

        // Se houver coleções (Inventory, Items), aplique estratégia de merge específica aqui.
        // Ex: sincronizar quantidades, adicionar itens novos, remover itens deletados.
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
            // Inicializar coleções vazias se necessário
        };
        return e;
    }
}
