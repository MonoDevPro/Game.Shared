using Arch.Core;
using Game.Core.Entities.Map;
using Game.Server.Headless.Core.ECS;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Services;
using Game.Server.Headless.Core.ECS.Game.Systems;
using Game.Server.Headless.Core.ECS.Game.Systems.Adapters;
using Game.Server.Headless.Core.ECS.Game.Systems.Persistence;
using Game.Server.Headless.Core.ECS.Game.Systems.Receive;
using Game.Server.Headless.Core.ECS.Game.Systems.Replication;
using Game.Server.Headless.Core.ECS.Game.Systems.Send;
using Game.Server.Headless.Core.ECS.Game.Systems.Simulation;
using Game.Server.Headless.Core.ECS.Game.Systems.Validation;
using Game.Server.Headless.Core.ECS.MainMenu.Systems.Accounts;
using Game.Server.Headless.Core.ECS.MainMenu.Systems.Characters;
using Game.Server.Headless.Infrastructure.Network;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.DbContexts;
using GameServer.Infrastructure.EfCore.Hasher;
using GameServer.Infrastructure.EfCore.Hosted;
using GameServer.Infrastructure.EfCore.Interceptors;
using GameServer.Infrastructure.EfCore.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.ECS;
using Shared.ECS.Groups;
using Shared.Network;
using Shared.Network.Repository;
using Shared.Network.Transport;

namespace Game.Server.Headless;

public static class Services
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // 1. Serviços Base
        services.AddSingleton(TimeProvider.System);
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Debug);
        });
        services.AddSingleton(new GameMap(100, 100));
        services.AddSingleton<ServerLoop>();
        services.AddSingleton<World>(_ =>
                World.Create(
                    chunkSizeInBytes: 16_384,               // 16 KB por chunk
                    minimumAmountOfEntitiesPerChunk: 100,   // Mínimo de 100 entidades por chunk
                    archetypeCapacity: 2,                   // Capacidade de 2 arquétipos por chunk
                    entityCapacity: 64)                     // Capacidade de 64 entidades por chunk
        );
        // Inicializa singletons ECS após criação do World
        services.PostConfigure<World>(world =>
        {
            // Player registry singleton
            var q = new QueryDescription().WithAll<PlayerRegistryComponent>();
            bool hasReg = false;
            world.Query(in q, (ref Entity e) => hasReg = true);
            if (!hasReg)
                world.Create(new PlayerRegistryComponent { PlayersByNetId = new Dictionary<int, Entity>() });
        });

        // 2. Serviços de Rede
        services.AddSingleton<INetLogger, LiteNetLibLogger>();
        services.AddSingleton<EventBasedNetListener>();
        services.AddSingleton(provider => new NetManager(provider.GetRequiredService<EventBasedNetListener>()));
        services.AddSingleton(_ => new NetPacketProcessor(NetworkConfigurations.MaxStringLength));
        services.AddSingleton<NetworkSender>();
        services.AddSingleton<NetworkReceiver>();
        services.AddSingleton<PeerRepository>();
        services.AddSingleton<NetworkManager, ServerNetwork>();
        // No explicit event bus; spawn events are queued via singleton component

        // Session service for account-peer binding
        services.AddSingleton<SessionService>();

        // Registra o serviço de hashing. 
        services.AddSingleton<IPasswordHasherService, BCryptPasswordHasherService>();
        // DbContext factory ( evita problemas de scope em BackgroundService )
        var connectionString = "Data Source=game_database.db"; // Exemplo de string de conexão
        // Registrar tanto o DbContext (para migrações) quanto a factory (para uso em background/threads)
        services.AddDbContext<GameDbContext>((sp, opt) =>
        {
            opt
                .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())
                .UseSqlite(connectionString, sqlite =>
                {
                    // As migrações residem no assembly GameServer.Infrastructure.EfCore
                    sqlite.MigrationsAssembly(typeof(GameDbContext).Assembly.GetName().Name);
                });
        });
        services.AddDbContextFactory<GameDbContext>((sp, opt) =>
        {
            opt
                .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())
                .UseSqlite(connectionString, sqlite =>
                {
                    // As migrações residem no assembly GameServer.Infrastructure.EfCore
                    sqlite.MigrationsAssembly(typeof(GameDbContext).Assembly.GetName().Name);
                });
        });

        // Repositórios EF
        services.AddSingleton<ICharacterRepository, CharacterRepository>();
        services.AddSingleton<IAccountRepository, AccountRepository>();
        // Background persistence wrapper (singleton) — Systems injetam este para enfileirar
        services.AddSingleton<IBackgroundPersistence, BackgroundPersistence>();
        // Worker HostedService que consome os readers do BackgroundPersistence
        services.AddHostedService<DatabaseWorker>();
        // HostedService que aplica as migrações do EF Core
        services.AddHostedService<ApplyMigrationsHosted>();
        // Interceptadores do EF Core
        services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();

        // Useful ECS services

        // Sistemas de Gestão de Entidades
        services.AddSingleton<PlayerLookupService>();

        // Sistemas de Rede - Recepção
        services.AddSingleton<NetworkPollSystem>();
        services.AddSingleton<NetworkToChatSystem>();
        services.AddSingleton<NetworkToAttackSystem>();
        services.AddSingleton<NetworkToMovementSystem>();
        // Adapters/Validation/Simulation/Replication
        services.AddSingleton<EnterGameAdapterSystem>();
        services.AddSingleton<ExitGameAdapterSystem>();
        services.AddSingleton<EnterGameValidationSystem>();
        services.AddSingleton<ExitGameValidationSystem>();
        services.AddSingleton<SpawnSystem>();
        services.AddSingleton<DespawnSystem>();
        services.AddSingleton<SpawnReplicationSystem>();
        services.AddSingleton<DespawnReplicationSystem>();

        services.AddSingleton<AccountCreationSystem>();
        services.AddSingleton<AccountLoginSystem>();
        services.AddSingleton<AccountLogoutSystem>();
        services.AddSingleton<CharacterListSystem>();
        services.AddSingleton<CharacterCreationSystem>();
        services.AddSingleton<CharacterSelectionSystem>();
        services.AddSingleton<PlayerSaveSystem>();
        services.AddSingleton(provider => new NetworkReceiveGroup(
        [
            provider.GetRequiredService<NetworkPollSystem>(),
            provider.GetRequiredService<NetworkToChatSystem>(),
            provider.GetRequiredService<NetworkToAttackSystem>(),
            provider.GetRequiredService<NetworkToMovementSystem>(),

            provider.GetRequiredService<AccountCreationSystem>(),
            provider.GetRequiredService<AccountLoginSystem>(),
            provider.GetRequiredService<AccountLogoutSystem>(),
            provider.GetRequiredService<CharacterListSystem>(),
            provider.GetRequiredService<CharacterCreationSystem>(),
            provider.GetRequiredService<CharacterSelectionSystem>(),
            // Game: network adapters -> validation -> simulation -> replication
            provider.GetRequiredService<EnterGameAdapterSystem>(),
            provider.GetRequiredService<ExitGameAdapterSystem>(),
            provider.GetRequiredService<EnterGameValidationSystem>(),
            provider.GetRequiredService<ExitGameValidationSystem>(),
            provider.GetRequiredService<SpawnSystem>(),
            provider.GetRequiredService<DespawnSystem>(),
            provider.GetRequiredService<SpawnReplicationSystem>(),
            provider.GetRequiredService<DespawnReplicationSystem>(),
            provider.GetRequiredService<PlayerSaveSystem>(),
        ]));

        // Sistemas de Rede - Envio
        // Não é adicionado neste grupo os sistemas que enviam, pois eles apenas adicionam o pacote no buffer,
        services.AddSingleton<NetworkFlushSystem>();
        services.AddSingleton(provider => new NetworkSendGroup(
        [
            // Adicione aqui sistemas relacionados ao envio de rede, se houver
            provider.GetRequiredService<NetworkFlushSystem>()
        ]));

        // Sistemas de Física
        services.AddSingleton<AttackSystem>();
        services.AddSingleton<MovementSystem>();
        services.AddSingleton(provider => new PhysicsSystemGroup(
        [
            provider.GetRequiredService<AttackSystem>(),
            provider.GetRequiredService<MovementSystem>(),
        ]));

        // Sistemas de Processamento Geral

        services.AddSingleton(provider => new ProcessSystemGroup(
        [
        ]));

        // Registrar o ECS Runner que vai executar os grupos de sistemas
        services.AddSingleton<EcsRunner>(provider => new AdapterEcsRunner(
            provider.GetRequiredService<ILogger<EcsRunner>>(),
            provider.GetRequiredService<World>(),
            provider.GetRequiredService<NetworkReceiveGroup>(),
            provider.GetRequiredService<PhysicsSystemGroup>(),
            provider.GetRequiredService<ProcessSystemGroup>(),
            provider.GetRequiredService<NetworkSendGroup>()
        ));

        return services;
    }
}