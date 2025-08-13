// Arquivos gerados: 
// 1) DatabaseWorker.cs
// 2) LoginSystem.cs
// 3) PlayerSaveSystem.cs
// --------------------------------------------------------
// OBS: Ajuste namespaces/imports conforme seu projeto.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Game.Server.Headless.Infrastructure.ECS.Components;
using Game.Server.Headless.Infrastructure.Persistence;
using Shared.Core.Network.Transport;
using Shared.Features.MainMenu.Account.AccountLogin;
using Shared.Features.Player.Components;
using Shared.Features.Player.Components.Tags;
using Shared.Infrastructure.ECS.Components;

// Define the IDatabaseService interface if missing
namespace Game.Server.Headless.Infrastructure.Persistence
{
    public interface IDatabaseService
    {
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task SavePlayerCharacterAsync(PlayerDataModel data);
    }
}

// Componentes usados para enfileirar pedidos de login e saves
public struct LoginRequestComponent
{
    public string Username;
    public string Password;
}
public struct SenderPeerComponent
{
    public int Value; // ID do peer que enviou o comando
}

// -----------------------------------------------------------------------------
// DTOs / Messages + PlayerDataModel
// -----------------------------------------------------------------------------
namespace Game.Server.Headless.Infrastructure.Persistence
{
    #region DTOs / Messages
    public sealed record LoginRequestMessage(int SenderPeer, string Username, string Password, Entity CommandEntity);
    public sealed record LoginResultMessage(int SenderPeer, bool Success, Entity CommandEntity, string? FailureReason);

    public sealed record SaveRequestMessage(Entity Entity, PlayerDataModel Data);
    public sealed record SaveResultMessage(Entity Entity, bool Success, Exception? Error);

    // Modelo simples - adapte com todos os campos necessários
    public sealed class PlayerDataModel
    {
        public int NetId { get; set; }
        public string? Name { get; set; }
        public int CurrentHealth { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        // adicione mais campos conforme necessidade
    }
    #endregion
}

// -----------------------------------------------------------------------------
// DirtyComponent (definição simples) e helpers de marcação
// -----------------------------------------------------------------------------
namespace Shared.Infrastructure.ECS.Components
{
    /// <summary>
    /// Marca que a entidade possui alterações que precisam ser persistidas.
    /// Componente vazio (tag component).
    /// </summary>
    public struct DirtyComponent { }
}

namespace Game.Server.Headless.Infrastructure.Persistence
{
    using Shared.Infrastructure.ECS.Components;

    public static class WorldDirtyExtensions
    {
        /// <summary>
        /// Marca a entidade como dirty (adiciona o componente se necessário).
        /// Uso: world.MarkDirty(entity);
        /// </summary>
        public static void MarkDirty(this World world, Entity entity)
        {
            if (!world.Has<DirtyComponent>(entity))
            {
                // Algumas APIs do Arch usam World.Add<T>(entity)
                // Se sua API divergir, ajuste aqui.
                world.Add<DirtyComponent>(entity);
            }
        }

        /// <summary>
        /// Remove a marca de dirty.
        /// </summary>
        public static void ClearDirty(this World world, Entity entity)
        {
            if (world.Has<DirtyComponent>(entity))
                world.Remove<DirtyComponent>(entity);
        }

        public static bool IsDirty(this World world, Entity entity) => world.Has<DirtyComponent>(entity);
    }
}

// -----------------------------------------------------------------------------
// DatabaseWorker
// -----------------------------------------------------------------------------
namespace Game.Server.Headless.Infrastructure.Persistence
{
    /// <summary>
    /// Background worker responsável por executar operações assíncronas de banco de dados.
    /// - Usa canais limitados (bounded channels) para aplicar backpressure.
    /// - Expõe Writers/Readers para os sistemas ECS enfileirarem pedidos e consumirem resultados.
    /// </summary>
    public sealed class DatabaseWorker : BackgroundService
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<DatabaseWorker> _logger;

        // Channels: bounded writers para requests, readers unbounded para resultados (ou bounded conforme desejar)
        private readonly Channel<LoginRequestMessage> _loginRequests;
        private readonly Channel<LoginResultMessage> _loginResults;

        private readonly Channel<SaveRequestMessage> _saveRequests;
        private readonly Channel<SaveResultMessage> _saveResults;

        // Controle de concorrência para saves
        private readonly SemaphoreSlim _saveConcurrency;

        public DatabaseWorker(IDatabaseService database, ILogger<DatabaseWorker> logger, int maxConcurrentSaves = 10, int loginQueueCapacity = 1024, int saveQueueCapacity = 8192)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _loginRequests = Channel.CreateBounded<LoginRequestMessage>(new BoundedChannelOptions(loginQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite
            });

            _loginResults = Channel.CreateUnbounded<LoginResultMessage>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

            _saveRequests = Channel.CreateBounded<SaveRequestMessage>(new BoundedChannelOptions(saveQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite
            });

            _saveResults = Channel.CreateUnbounded<SaveResultMessage>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

            _saveConcurrency = new SemaphoreSlim(maxConcurrentSaves);
        }

        // Expor writers/readers para injeção nos sistemas
        public ChannelWriter<LoginRequestMessage> LoginRequestWriter => _loginRequests.Writer;
        public ChannelReader<LoginResultMessage> LoginResultReader => _loginResults.Reader;

        public ChannelWriter<SaveRequestMessage> SaveRequestWriter => _saveRequests.Writer;
        public ChannelReader<SaveResultMessage> SaveResultReader => _saveResults.Reader;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Inicia loops de processamento (executam até o cancelamento)
            var loginLoop = Task.Run(() => ProcessLoginsAsync(stoppingToken), stoppingToken);
            var saveLoop = Task.Run(() => ProcessSavesAsync(stoppingToken), stoppingToken);

            await Task.WhenAll(loginLoop, saveLoop).ConfigureAwait(false);
        }

        private async Task ProcessLoginsAsync(CancellationToken ct)
        {
            await foreach (var req in _loginRequests.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    // Execute a validação no DB (async)
                    bool ok = await _database.ValidateCredentialsAsync(req.Username, req.Password).ConfigureAwait(false);
                    var result = new LoginResultMessage(req.SenderPeer, ok, req.CommandEntity, ok ? null : "Invalid credentials");
                    // Não bloqueia - grava resultado para consumo pelo ECS
                    await _loginResults.Writer.WriteAsync(result, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing login for {User}", req.Username);
                    var result = new LoginResultMessage(req.SenderPeer, false, req.CommandEntity, ex.Message);
                    await _loginResults.Writer.WriteAsync(result, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessSavesAsync(CancellationToken ct)
        {
            await foreach (var req in _saveRequests.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                // Cada request dispara um worker controlado por semáforo
                await _saveConcurrency.WaitAsync(ct).ConfigureAwait(false);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _database.SavePlayerCharacterAsync(req.Data).ConfigureAwait(false);
                        await _saveResults.Writer.WriteAsync(new SaveResultMessage(req.Entity, true, null), CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Save failed for entity {Entity}", req.Entity);
                        await _saveResults.Writer.WriteAsync(new SaveResultMessage(req.Entity, false, ex), CancellationToken.None).ConfigureAwait(false);
                    }
                    finally
                    {
                        _saveConcurrency.Release();
                    }
                }, ct);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DatabaseWorker stopping. Waiting pending saves to complete...");
            // Opcional: aguardar tasks atuais com timeout
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

// --------------------------------------------------------
// LoginSystem: enfileira pedidos de login e consome resultados no Update (thread do ECS)
// --------------------------------------------------------
namespace Game.Server.Headless.Infrastructure.ECS.Systems.Process
{
    public partial class LoginSystem : BaseSystem<World, float>
    {
        private readonly ChannelWriter<Game.Server.Headless.Infrastructure.Persistence.LoginRequestMessage> _loginWriter;
        private readonly ChannelReader<Game.Server.Headless.Infrastructure.Persistence.LoginResultMessage> _loginResultReader;
        private readonly NetworkSender _sender;
        private readonly ILogger<LoginSystem> _logger;

        // buffer local para processar results no Update sem depender do Channel.ReadAllAsync
        private readonly ConcurrentQueue<Game.Server.Headless.Infrastructure.Persistence.LoginResultMessage> _pendingResults = new();

        public LoginSystem(World world, DatabaseWorker dbWorker, NetworkSender sender, ILogger<LoginSystem> logger) : base(world)
        {
            _loginWriter = dbWorker.LoginRequestWriter;
            _loginResultReader = dbWorker.LoginResultReader;
            _sender = sender;
            _logger = logger;
        }

        // Query: apenas enfileira o pedido (rápido)
        [Query]
        [All<LoginRequestComponent, SenderPeerComponent>]
        private void EnqueueLoginRequest(in Entity commandEntity, ref LoginRequestComponent request, ref SenderPeerComponent sender)
        {
            var msg = new Game.Server.Headless.Infrastructure.Persistence.LoginRequestMessage(sender.Value, request.Username, request.Password, commandEntity);
            // TryWrite para não bloquear o loop ECS. Se o channel estiver cheio, registramos e descartamos ou respondemos erro.
            if (!_loginWriter.TryWrite(msg))
            {
                _logger.LogWarning("Login queue full — dropping login request for {User}", request.Username);
                // Opcional: responder ao cliente que o servidor está ocupado
                var busyResponse = new AccountLoginResponse { Success = false, Message = "Server busy" };
                _sender.EnqueueReliableSend(sender.Value, ref busyResponse);

                // Também destruímos o comando para evitar leak se desejar
                if (World.IsAlive(commandEntity)) World.Destroy(commandEntity);
            }
        }

        public override void Update(in float dt)
        {
            // Transferir resultados do Channel para fila local (não bloquear)
            while (_loginResultReader.TryRead(out var res))
                _pendingResults.Enqueue(res);

            // Processar resultados (aplicar no World e enviar rede)
            while (_pendingResults.TryDequeue(out var r))
            {
                try
                {
                    if (r.Success)
                    {
                        // Aqui crie a entidade do jogador / componentes necessários
                        // Exemplo simples: enviar resposta de sucesso
                        var response = new AccountLoginResponse { Success = true };
                        _sender.EnqueueReliableSend(r.SenderPeer, ref response);

                        _logger.LogInformation("Login success for peer {Peer}", r.SenderPeer);
                    }
                    else
                    {
                        var response = new AccountLoginResponse { Success = false, Message = r.FailureReason };
                        _sender.EnqueueReliableSend(r.SenderPeer, ref response);
                        _logger.LogInformation("Login failed for peer {Peer}: {Reason}", r.SenderPeer, r.FailureReason);
                    }

                    // destruir o comando de login (se ainda existir)
                    if (World.IsAlive(r.CommandEntity)) World.Destroy(r.CommandEntity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying login result for peer {Peer}", r.SenderPeer);
                }
            }
        }
    }
}

// --------------------------------------------------------
// PlayerSaveSystem: enfileira saves e consome resultados para remover DirtyComponent após sucesso
// --------------------------------------------------------
namespace Game.Server.Headless.Infrastructure.ECS.Systems.Process
{
    public partial class PlayerSaveSystem : BaseSystem<World, float>
    {
        private readonly ChannelWriter<Game.Server.Headless.Infrastructure.Persistence.SaveRequestMessage> _saveWriter;
        private readonly ChannelReader<Game.Server.Headless.Infrastructure.Persistence.SaveResultMessage> _saveResultReader;
        private readonly ILogger<PlayerSaveSystem> _logger;

        private float _timer = 0f;
        private const float SAVE_INTERVAL = 300f; // 5 minutos

        // Buffer local para processar resultados sem bloquear a leitura do Channel
        private readonly ConcurrentQueue<Game.Server.Headless.Infrastructure.Persistence.SaveResultMessage> _resultsQueue = new();

        public PlayerSaveSystem(World world, DatabaseWorker worker, ILogger<PlayerSaveSystem> logger) : base(world)
        {
            _saveWriter = worker.SaveRequestWriter;
            _saveResultReader = worker.SaveResultReader;
            _logger = logger;
        }

        public override void Update(in float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < SAVE_INTERVAL)
            {
                // Ainda consumimos resultados pendentes a cada Update para remover Dirty quando saves terminarem
                DrainSaveResultsQueue();
                return;
            }

            _timer = 0f; // reset

            var query = new QueryDescription().WithAll<PlayerTag, DirtyComponent, GridPositionComponent, HealthComponent, PlayerInfoComponent>();

            World.Query(in query, (Entity entity, ref PlayerTag tag, ref DirtyComponent dirty, ref GridPositionComponent pos, ref HealthComponent health, ref PlayerInfoComponent info) =>
            {
                // Monta o modelo de dados para salvar
                var model = new Game.Server.Headless.Infrastructure.Persistence.PlayerDataModel
                {
                    NetId = World.Get<NetworkedTag>(entity).Id,
                    Name = info.Name,
                    CurrentHealth = health.Current,
                    PositionX = pos.Value.X,
                    PositionY = pos.Value.Y
                };

                var req = new Game.Server.Headless.Infrastructure.Persistence.SaveRequestMessage(entity, model);

                if (!_saveWriter.TryWrite(req))
                {
                    _logger.LogWarning("Save queue full — skipping save for entity {Entity}", entity);
                    // opcional: deixar Dirty para próxima vez
                }
            });

            // Após enfileirar requests, processamos resultados pendentes
            DrainSaveResultsQueue();
        }

        private void DrainSaveResultsQueue()
        {
            while (_saveResultReader.TryRead(out var r))
                _resultsQueue.Enqueue(r);

            while (_resultsQueue.TryDequeue(out var res))
            {
                try
                {
                    if (res.Success)
                    {
                        if (World.IsAlive(res.Entity))
                        {
                            // Remove DirtyComponent agora que o save foi bem sucedido
                            World.Remove<DirtyComponent>(res.Entity);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(res.Error, "Save failed for entity {Entity}. Will retry next interval.", res.Entity);
                        // política: re-enfileirar, marcar para retry manual, etc.
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying save result for entity {Entity}", res.Entity);
                }
            }
        }
    }
}

// --------------------------------------------------------
// Exemplos de integração: quando marcar Dirty
// --------------------------------------------------------
/*
 - MovementSystem: marque Dirty quando a entidade troca de célula (ou quando o jogador se move uma distância significativa).
    Exemplo (pseudocódigo dentro do MovementSystem):

    var oldCell = interest.GetCellFor(entity);
    var newCell = ComputeCellKey(newPosition);
    if (oldCell != newCell)
    {
        World.MarkDirty(entity); // extension method adicionada no arquivo
        interest.NotifyEntityMoved(entity);
    }

 - Health/Combat: quando Health for alterado (dano/recuperação) marque Dirty.
 - Inventory/Equipment: marque Dirty ao equip/unequip ou trocar inventário.
 - Login: após criar a entidade do jogador (first spawn), considere marcar Dirty para garantir save inicial.
*/

// --------------------------------------------------------
// INSTRUÇÕES DE DI (adicionar ao Program.cs / Startup)
// --------------------------------------------------------
/*
// Registrar serviços e worker
builder.Services.AddSingleton<DatabaseWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseWorker>());

// Registre o seu IDatabaseService (implementação concreta)
builder.Services.AddSingleton<IDatabaseService, YourDatabaseServiceImplementation>();

// Registre sistemas ECS (depende de como você monta o World/Systems)
// Exemplo: se usa DI para criar sistemas
builder.Services.AddTransient<LoginSystem>();
builder.Services.AddTransient<PlayerSaveSystem>();

// Nota: injete ILogger<T> automaticamente pelo DI
*/
// --------------------------------------------------------
// MovementSystem: detecta troca de célula e marca Dirty + notifica InterestSystem
// --------------------------------------------------------
namespace Game.Server.Headless.Infrastructure.ECS.Systems.Process
{
    using Game.Server.Headless.Infrastructure.Persistence; // traz MarkDirty

    public partial class MovementSystem : BaseSystem<World, float>
    {
        private readonly InterestSystem _interest;
        private readonly ILogger<MovementSystem> _logger;
        private readonly int _cellSizeTiles;

        // Mapa NetId -> last known cellKey
        private readonly Dictionary<int, long> _lastCell = new();

        public MovementSystem(World world, InterestSystem interest, ILogger<MovementSystem> logger, int cellSizeTiles = 8) : base(world)
        {
            _interest = interest ?? throw new ArgumentNullException(nameof(interest));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cellSizeTiles = Math.Max(1, cellSizeTiles);
        }

        public override void Update(in float dt)
        {
            // Query por entidades networked que possuem GridPosition
            var query = new QueryDescription().WithAll<NetworkedTag, GridPositionComponent>();

            World.Query(in query, (Entity entity, ref NetworkedTag netTag, ref GridPositionComponent pos) =>
            {
                var netId = netTag.Id;
                var newCell = ComputeCellKey(pos.Value);

                if (_lastCell.TryGetValue(netId, out var oldCell))
                {
                    if (oldCell != newCell)
                    {
                        // atualiza cache
                        _lastCell[netId] = newCell;

                        // marca para salvar e notifica InterestSystem
                        World.MarkDirty(entity);
                        try
                        {
                            _interest.NotifyEntityMoved(entity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error notifying InterestSystem for entity {NetId}", netId);
                        }
                    }
                }
                else
                {
                    // primeira vez que vemos essa entidade - registrar cell
                    _lastCell[netId] = newCell;
                }
            });
        }

        private long ComputeCellKey(dynamic gridPosition)
        {
            // normaliza para inteiros de célula
            int cellX = (int)Math.Floor((double)(gridPosition.X) / _cellSizeTiles);
            int cellY = (int)Math.Floor((double)(gridPosition.Y) / _cellSizeTiles);
            return PackCell(cellX, cellY);
        }

        private static long PackCell(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}

