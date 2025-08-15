using System.Collections.Concurrent;
using GameServer.Infrastructure.EfCore.Repositories;
using GameServer.Infrastructure.EfCore.Worker.Models;
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
                    var repo = scope.ServiceProvider.GetRequiredService<ICharacterRepository>();

                    try
                    {
                        var (success, player, error) = await repo.ValidateLoginAsync(req.Username, req.PasswordHash, stoppingToken).ConfigureAwait(false);
                        var res = new LoginResult(req.CommandId, req.SenderPeer, success, player, error);
                        await _background.PublishLoginResultAsync(res, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao validar login.");
                        var res = new LoginResult(req.CommandId, req.SenderPeer, false, null, ex.Message);
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
}