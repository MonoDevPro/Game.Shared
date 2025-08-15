using System.Collections.Concurrent;
using System.Threading.Channels;
using Arch.Core;
using Arch.System;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Tags;
using Game.Core.Entities.Character;
using Game.Server.Headless.Core.ECS.Components;
using Game.Server.Headless.Core.ECS.Persistence;
using GameServer.Infrastructure.EfCore;
using GameServer.Infrastructure.EfCore.Worker;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless.Core.ECS.Systems.Persistence;

public partial class PlayerSaveSystem : BaseSystem<World, float>
    {
        private readonly ChannelWriter<SaveRequestMessage> _saveWriter;
        private readonly ChannelReader<SaveResultMessage> _saveResultReader;
        private readonly ILogger<PlayerSaveSystem> _logger;

        private float _timer = 0f;
        private const float SAVE_INTERVAL = 300f; // 5 minutos

        // Buffer local para processar resultados sem bloquear a leitura do Channel
        private readonly ConcurrentQueue<SaveResultMessage> _resultsQueue = new();

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

            var query = new QueryDescription().WithAll<NetworkedTag, ECS.Persistence.Components, MapPositionComponent, CharInfoComponent>();

            World.Query(in query, (Entity entity, ref NetworkedTag tag, ref ECS.Persistence.Components dirty, ref MapPositionComponent pos, ref CharInfoComponent info) =>
            {
                // Monta o modelo de dados para salvar
                var model = new CharacterEntity()
                {
                    NetId = World.Get<NetworkedTag>(entity).Id,
                    Name = info.Name,
                    PositionX = pos.Value.X,
                    PositionY = pos.Value.Y
                };

                var req = new SaveRequestMessage(entity, model);

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
                            World.Remove<ECS.Persistence.Components>(res.Entity);
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