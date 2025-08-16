using System.Reflection;
using Arch.Core;
using Game.Core.ECS;
using Game.Core.ECS.Groups;
using Game.Core.Entities.Account;
using Game.Core.Entities.Character;
using Game.Core.Entities.Map;
using Game.Server.Headless.Core.ECS.Persistence.Systems;
using Game.Server.Headless.Core.ECS.Systems;
using Game.Server.Headless.Core.ECS.Systems.Receive;
using Game.Server.Headless.Core.ECS.Systems.Send;
using Game.Server.Headless.Infrastructure.MainMenu.Receive;
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
using Shared.Core.Common.Constants;
using Shared.Core.Network;
using Shared.Core.Network.Repository;
using Shared.Core.Network.Transport;

namespace Game.Server.Headless;

public static class Services
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // 1. Serviços Base
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

        // 2. Serviços de Rede
        services.AddSingleton<INetLogger, LiteNetLibLogger>();
        services.AddSingleton<EventBasedNetListener>();
        services.AddSingleton(provider => new NetManager(provider.GetRequiredService<EventBasedNetListener>()));
        services.AddSingleton(_ => new NetPacketProcessor(NetworkConfigurations.MaxStringLength));
        services.AddSingleton<NetworkSender>();
        services.AddSingleton<NetworkReceiver>();
        services.AddSingleton<PeerRepository>();
        services.AddSingleton<NetworkManager, ServerNetwork>();

        // Session service for account-peer binding
        services.AddSingleton<SessionService>();

        // Registra o serviço de hashing. 
        services.AddSingleton<IPasswordHasherService, BCryptPasswordHasherService>();
        // DbContext factory ( evita problemas de scope em BackgroundService )
        var connectionString = "Data Source=game_database.db"; // Exemplo de string de conexão
        services.AddDbContextFactory<GameDbContext>((sp, opt) =>
        {
            opt
                .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())
                .UseSqlite(connectionString, sqlite =>
                {
                    sqlite.MigrationsAssembly(Assembly.GetExecutingAssembly());
                });
        });

        // Repositórios EF
        services.AddScoped<GameServer.Infrastructure.EfCore.Repositories.ICharacterRepository, GameServer.Infrastructure.EfCore.Repositories.CharacterRepository>();
        services.AddScoped<GameServer.Infrastructure.EfCore.Repositories.IAccountRepository, GameServer.Infrastructure.EfCore.Repositories.AccountRepository>();

        // Background persistence wrapper (singleton) — Systems injetam este para enfileirar
        services.AddSingleton<IBackgroundPersistence, BackgroundPersistence>();

        // Worker HostedService que consome os readers do BackgroundPersistence
        services.AddHostedService<DatabaseWorker>();

        // HostedService que aplica as migrações do EF Core
        services.AddHostedService<ApplyMigrationsHosted>();

        // Interceptadores do EF Core
        services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();

        // Sistemas de Gestão de Entidades
        services.AddSingleton<EntitySystem>();

        // Sistemas de Rede - Recepção
        services.AddSingleton<NetworkPollSystem>();
        services.AddSingleton<MainMenuReceiveSystem>();
        services.AddSingleton<NetworkToChatSystem>();
        services.AddSingleton<NetworkToEntitySystem>();
        services.AddSingleton<NetworkToMovementSystem>();
        services.AddSingleton(provider => new NetworkReceiveGroup(
        [
            provider.GetRequiredService<NetworkPollSystem>(),
            provider.GetRequiredService<MainMenuReceiveSystem>(),
            provider.GetRequiredService<NetworkToChatSystem>(),
            provider.GetRequiredService<NetworkToEntitySystem>(),
            provider.GetRequiredService<NetworkToMovementSystem>(),
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
        services.AddSingleton<MovementStartSystem>();
        services.AddSingleton<MovementProcessSystem>();
        services.AddSingleton<LoginSystem>();
        services.AddSingleton<LoginResultSystem>();
        services.AddSingleton<PlayerSaveSystem>();
        services.AddSingleton<SaveResultSystem>();
        services.AddSingleton(provider => new PhysicsSystemGroup(
            [
                provider.GetRequiredService<MovementStartSystem>(),
            provider.GetRequiredService<MovementProcessSystem>(),
        ]));

        // Sistemas de Processamento Geral
        services.AddSingleton(provider => new ProcessSystemGroup(
        [
            provider.GetRequiredService<LoginSystem>(),
            provider.GetRequiredService<LoginResultSystem>(),
            provider.GetRequiredService<PlayerSaveSystem>(),
            provider.GetRequiredService<SaveResultSystem>(),
        ]));

        // Registrar o ECS Runner que vai executar os grupos de sistemas
        services.AddSingleton<EcsRunner>(provider => new EcsRunner(
            provider.GetRequiredService<ILogger<EcsRunner>>(),
            provider.GetRequiredService<NetworkReceiveGroup>(),
            provider.GetRequiredService<PhysicsSystemGroup>(),
            provider.GetRequiredService<ProcessSystemGroup>(),
            provider.GetRequiredService<NetworkSendGroup>()
        ));

        return services;
    }
}