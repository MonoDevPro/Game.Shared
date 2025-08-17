using System.Collections.Concurrent;
using Game.Core.Common.Enums;
using Game.Core.Common.Rules;
using Game.Core.Common.ValueObjetcs;
using GameServer.Infrastructure.EfCore.DbContexts;
using GameServer.Infrastructure.EfCore.Repositories;
using GameServer.Infrastructure.EfCore.Worker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Worker;
/// <summary>
/// Background worker que consome requests do BackgroundPersistence e realiza operações de DB
/// sem bloquear o loop do ECS.
/// </summary>
public sealed class DatabaseWorker : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<DatabaseWorker> _logger;
    private readonly IBackgroundPersistence _background;

    // controle de concorrência e inflight tasks
    private readonly SemaphoreSlim _saveConcurrency;
    private readonly ConcurrentBag<Task> _inflightTasks = new();

    // parâmetros de batching / tune conforme sua carga
    private readonly int _saveBatchSize;
    private readonly TimeSpan _saveBatchInterval;
    private readonly int _maxSaveConcurrency;

    public DatabaseWorker(
        IServiceProvider provider,
        IBackgroundPersistence background,
        ILogger<DatabaseWorker> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _background = background ?? throw new ArgumentNullException(nameof(background));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ajustes padrão (mude após benchmark)
        _saveBatchSize = 200;
        _saveBatchInterval = TimeSpan.FromSeconds(2);
        _maxSaveConcurrency = 4;

        _saveConcurrency = new SemaphoreSlim(_maxSaveConcurrency, _maxSaveConcurrency);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // iniciar loops sem bloquear ExecuteAsync
        _ = Task.Run(() => ProcessSavesLoopAsync(stoppingToken), CancellationToken.None);
        _ = Task.Run(() => ProcessLoginsLoopAsync(stoppingToken), CancellationToken.None);
        _ = Task.Run(() => ProcessAccountCreationLoopAsync(stoppingToken), CancellationToken.None);
        _ = Task.Run(() => ProcessCharacterListLoopAsync(stoppingToken), CancellationToken.None);
        _ = Task.Run(() => ProcessCharacterCreationLoopAsync(stoppingToken), CancellationToken.None);
        _ = Task.Run(() => ProcessCharacterSelectionLoopAsync(stoppingToken), CancellationToken.None);

        _logger.LogInformation("DatabaseWorker started.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loop que consome SaveRequest do wrapper, agrupa em batches e dispara processamento.
    /// </summary>
    private async Task ProcessSavesLoopAsync(CancellationToken stoppingToken)
    {
        var reader = _background.SaveRequestsReader;

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                // coleto o primeiro item (bloqueante)
                var batch = new List<SaveRequest>(_saveBatchSize);
                var first = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                batch.Add(first);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                // coletar até atingir batch size ou tempo limite
                while (batch.Count < _saveBatchSize && sw.Elapsed < _saveBatchInterval)
                {
                    if (reader.TryRead(out var next))
                    {
                        batch.Add(next);
                        continue;
                    }

                    // pequeno aguardo para preencher o batch (poll window)
                    try { await Task.Delay(50, stoppingToken).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }

                // aguarda disponibilidade de slot de concorrência
                await _saveConcurrency.WaitAsync(stoppingToken).ConfigureAwait(false);

                // processa batch em tarefa separada (não bloquear loop)
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessSaveBatchAsync(batch, stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception ao processar batch de saves.");
                        // publicar falha para cada req
                        foreach (var r in batch)
                        {
                            var res = new SaveResult(r.CommandId, r.CharacterId, false, ex.Message);
                            try { await _background.PublishSaveResultAsync(res, CancellationToken.None).ConfigureAwait(false); } catch { /* swallow */ }
                        }
                    }
                    finally
                    {
                        _saveConcurrency.Release();
                    }
                }, CancellationToken.None);

                _inflightTasks.Add(task);
            }
        }
        catch (OperationCanceledException)
        {
            // cancelamento esperado
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loop de saves interrompido por exceção.");
        }
        finally
        {
            _logger.LogInformation("ProcessSavesLoopAsync finalizado.");
        }
    }

    /// <summary>
    /// Processa efetivamente um batch de SaveRequest: cria scope, resolve IPlayerRepository e chama SavePlayersBatchAsync.
    /// </summary>
    private async Task ProcessSaveBatchAsync(List<SaveRequest> batch, CancellationToken ct)
    {
        if (batch == null || batch.Count == 0) return;

        var saves = new List<CharacterSaveModel>(batch.Count);
        foreach (var req in batch) saves.Add(req.Data);

        // criar scope para obter repositório (DbContext por operação)
        using var scope = _provider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICharacterRepository>();

        try
        {
            await repo.SaveCharactersBatchAsync(saves, ct).ConfigureAwait(false);

            // publicar sucesso para cada request
            foreach (var req in batch)
            {
                var res = new SaveResult(req.CommandId, req.CharacterId, true, null);
                await _background.PublishSaveResultAsync(res, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao salvar batch de players.");
            foreach (var req in batch)
            {
                var res = new SaveResult(req.CommandId, req.CharacterId, false, ex.Message);
                try { await _background.PublishSaveResultAsync(res, CancellationToken.None).ConfigureAwait(false); }
                catch (Exception pubEx) { _logger.LogWarning(pubEx, "Falha ao publicar SaveResult após erro de save."); }
            }
        }
    }

    /// <summary>
    /// Loop que consome LoginRequest do wrapper e valida credenciais (ou carrega player) usando repositório.
    /// </summary>
    private async Task ProcessLoginsLoopAsync(CancellationToken stoppingToken)
    {
        var reader = _background.LoginRequestsReader;

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                var req = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);

                // processa cada login em task separada para não bloquear leitura
                var t = Task.Run(async () =>
                {
                    using var scope = _provider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

                    try
                    {
                        var (success, accountId, player, error) = await repo.ValidateLoginAsync(req.Username, req.PasswordPlainText, stoppingToken).ConfigureAwait(false);
                        var res = new LoginResult(req.CommandId, req.SenderPeer, success, accountId, player, error);
                        await _background.PublishLoginResultAsync(res, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao validar login.");
                        var res = new LoginResult(req.CommandId, req.SenderPeer, false, null, null, ex.Message);
                        try { await _background.PublishLoginResultAsync(res, CancellationToken.None).ConfigureAwait(false); } catch { /* swallow */ }
                    }
                }, CancellationToken.None);

                _inflightTasks.Add(t);
            }
        }
        catch (OperationCanceledException)
        {
            // stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loop de logins interrompido por exceção.");
        }
        finally
        {
            _logger.LogInformation("ProcessLoginsLoopAsync finalizado.");
        }
    }

    private async Task ProcessAccountCreationLoopAsync(CancellationToken stoppingToken)
    {
        var reader = _background.AccountCreationRequestsReader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                var req = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                var t = Task.Run(async () =>
                {
                    using var scope = _provider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
                    try
                    {
                        var username = (req.Username ?? string.Empty).Trim();
                        var email = (req.Email ?? string.Empty).Trim();
                        var password = req.PasswordPlainTexto ?? string.Empty;

                        // Single source of truth: validate here against domain/DB constraints
                        if (!UsernameRule.IsValid(username))
                        {
                            var resInvalid = new AccountCreationResult(req.CommandId, req.SenderPeer, false, UsernameRule.Description);
                            await _background.PublishAccountCreationResultAsync(resInvalid, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }
                        if (!EmailRule.IsValid(email))
                        {
                            var resInvalid = new AccountCreationResult(req.CommandId, req.SenderPeer, false, EmailRule.Description);
                            await _background.PublishAccountCreationResultAsync(resInvalid, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }
                        if (!PasswordRule.IsValid(password))
                        {
                            var resInvalid = new AccountCreationResult(req.CommandId, req.SenderPeer, false, PasswordRule.Description);
                            await _background.PublishAccountCreationResultAsync(resInvalid, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }

                        var (success, accountId, error) = await repo.CreateAccountAsync(username, email, password, stoppingToken).ConfigureAwait(false);
                        var res = new AccountCreationResult(req.CommandId, req.SenderPeer, success, error);
                        await _background.PublishAccountCreationResultAsync(res, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao criar conta");
                        var res = new AccountCreationResult(req.CommandId, req.SenderPeer, false, ex.Message);
                        try { await _background.PublishAccountCreationResultAsync(res, CancellationToken.None).ConfigureAwait(false); } catch { }
                    }
                }, CancellationToken.None);
                _inflightTasks.Add(t);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loop de criação de contas interrompido por exceção.");
        }
    }

    /// <summary>
    /// Aguarda tarefas em voo (com timeout) e finaliza. Não manipula canais do wrapper.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseWorker stopping: awaiting inflight tasks...");

        var timeout = TimeSpan.FromSeconds(20);
        try
        {
            var tasks = _inflightTasks.ToArray();
            if (tasks.Length > 0)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                await Task.WhenAll(tasks).WaitAsync(cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timeout aguardando tasks inflight do DatabaseWorker.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro aguardando tasks inflight do DatabaseWorker.");
        }

        _logger.LogInformation("DatabaseWorker stopped.");
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static CharacterSummaryModel ToSummary(Game.Core.Entities.Character.CharacterEntity e) => new()
    {
        CharacterId = e.Id,
        Name = e.Name,
        Vocation = e.Vocation,
        Gender = e.Gender,
    };

    private async Task ProcessCharacterListLoopAsync(CancellationToken stoppingToken)
    {
        var reader = _background.CharacterListRequestsReader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                var req = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                var t = Task.Run(async () =>
                {
                    using var scope = _provider.CreateScope();
                    var dbf = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GameDbContext>>();
                    await using var db = await dbf.CreateDbContextAsync(stoppingToken);
                    try
                    {
                        var list = await db.Characters.AsNoTracking()
                            .Where(c => c.AccountId == req.AccountId)
                            .Select(c => new CharacterSummaryModel { CharacterId = c.Id, Name = c.Name, Vocation = c.Vocation, Gender = c.Gender })
                            .ToListAsync(stoppingToken).ConfigureAwait(false);
                        var res = new CharacterListResult(req.CommandId, req.SenderPeer, list);
                        await _background.PublishCharacterListResultAsync(res, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao listar personagens");
                        var res = new CharacterListResult(req.CommandId, req.SenderPeer, Array.Empty<CharacterSummaryModel>());
                        try { await _background.PublishCharacterListResultAsync(res, CancellationToken.None).ConfigureAwait(false); } catch { }
                    }
                }, CancellationToken.None);
                _inflightTasks.Add(t);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loop de listagem de personagens interrompido por exceção.");
        }
    }

    private async Task ProcessCharacterCreationLoopAsync(CancellationToken stoppingToken)
    {
        var reader = _background.CharacterCreationRequestsReader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                var req = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                var t = Task.Run(async () =>
                {
                    using var scope = _provider.CreateScope();
                    var dbf = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GameDbContext>>();
                    await using var db = await dbf.CreateDbContextAsync(stoppingToken);
                    try
                    {
                        var name = (req.Name ?? string.Empty).Trim();
                        if (!CharacterNameRule.TryValidate(name, out var nameError))
                        {
                            var denied = new CharacterCreationResult(req.CommandId, req.SenderPeer, false, null, nameError ?? CharacterNameRule.Description);
                            await _background.PublishCharacterCreationResultAsync(denied, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }
                        // checks: per-account count and unique name
                        var count = await db.Characters.AsNoTracking().CountAsync(c => c.AccountId == req.AccountId, stoppingToken);
                        if (count >= Game.Core.Entities.Character.CharacterConstants.MaxCharacterCount)
                        {
                            var denied = new CharacterCreationResult(req.CommandId, req.SenderPeer, false, null, $"You can only have up to {Game.Core.Entities.Character.CharacterConstants.MaxCharacterCount} characters.");
                            await _background.PublishCharacterCreationResultAsync(denied, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }

                        var exists = await db.Characters.AsNoTracking().AnyAsync(c => c.Name == name, stoppingToken);
                        if (exists)
                        {
                            var denied = new CharacterCreationResult(req.CommandId, req.SenderPeer, false, null, "Character name already in use.");
                            await _background.PublishCharacterCreationResultAsync(denied, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }

                        var entity = new Game.Core.Entities.Character.CharacterEntity
                        {
                            AccountId = req.AccountId,
                            Name = name,
                            Vocation = req.Vocation,
                            Gender = req.Gender,
                            Direction = DirectionEnum.South,
                            Position = new MapPosition(5, 5),
                            Speed = 1.0f,
                        };
                        await db.Characters.AddAsync(entity, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken);

                        var summary = new CharacterSummaryModel { CharacterId = entity.Id, Name = entity.Name, Vocation = entity.Vocation, Gender = entity.Gender };
                        var res = new CharacterCreationResult(req.CommandId, req.SenderPeer, true, summary, null);
                        await _background.PublishCharacterCreationResultAsync(res, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao criar personagem");
                        var res = new CharacterCreationResult(req.CommandId, req.SenderPeer, false, null, ex.Message);
                        try { await _background.PublishCharacterCreationResultAsync(res, CancellationToken.None).ConfigureAwait(false); } catch { }
                    }
                }, CancellationToken.None);
                _inflightTasks.Add(t);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loop de criação de personagem interrompido por exceção.");
        }
    }

    private async Task ProcessCharacterSelectionLoopAsync(CancellationToken stoppingToken)
    {
        var reader = _background.CharacterSelectionRequestsReader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                var req = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                var t = Task.Run(async () =>
                {
                    using var scope = _provider.CreateScope();
                    var dbf = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GameDbContext>>();
                    await using var db = await dbf.CreateDbContextAsync(stoppingToken);
                    try
                    {
                        var ch = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.CharacterId, stoppingToken);
                        if (ch is null || ch.AccountId != req.AccountId)
                        {
                            var denied = new CharacterSelectionResult(req.CommandId, req.SenderPeer, false, null, "Character not found");
                            await _background.PublishCharacterSelectionResultAsync(denied, CancellationToken.None).ConfigureAwait(false);
                            return;
                        }
                        var summary = new CharacterSummaryModel { CharacterId = ch.Id, Name = ch.Name, Vocation = ch.Vocation, Gender = ch.Gender };
                        var ok = new CharacterSelectionResult(req.CommandId, req.SenderPeer, true, summary, null);
                        await _background.PublishCharacterSelectionResultAsync(ok, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao selecionar personagem");
                        var res = new CharacterSelectionResult(req.CommandId, req.SenderPeer, false, null, ex.Message);
                        try { await _background.PublishCharacterSelectionResultAsync(res, CancellationToken.None).ConfigureAwait(false); } catch { }
                    }
                }, CancellationToken.None);
                _inflightTasks.Add(t);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loop de seleção de personagem interrompido por exceção.");
        }
    }
}