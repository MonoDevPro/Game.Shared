using Arch.Core;
using Game.Server.Headless.Infrastructure.ECS.Systems.Physics;
using Game.Server.Headless.Infrastructure.ECS.Systems.Process;
using Game.Server.Headless.Infrastructure.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS;
using Shared.Infrastructure.ECS.Groups;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;
using Shared.Infrastructure.World;

namespace Game.Server.Headless;

public static class Services
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddLogging(configure => configure.AddConsole());
            
        // Register network services
        services.AddNetworkServices();
        
        // Register ECS systems
        AddEcsSystems(services);    
        
        // Adiciona a lógica do mapa
        services.AddSingleton<GameMap>(new GameMap(100, 100)); // Carrega o mapa aqui
        
        // Adiciona a nossa nova classe ServerLoop como um serviço
        services.AddSingleton<ServerLoop>();

        return services;
    }
    
    private static void AddNetworkServices(this IServiceCollection services)
    {
        // Registra os serviços de rede / shared
        services.AddSingleton<NetworkManager, ServerNetwork>();
        services.AddSingleton<NetworkSender>( p => p.GetRequiredService<NetworkManager>().Sender);
        services.AddSingleton<NetworkReceiver>(p => p.GetRequiredService<NetworkManager>().Receiver);
        services.AddSingleton<PeerRepository>(p => p.GetRequiredService<NetworkManager>().PeerRepository);
    }
    
    private static void AddEcsSystems(this IServiceCollection services)
    {
        // Registrar o World do ECS
        services.AddSingleton<World>(_ =>
            World.Create(
                chunkSizeInBytes: 16_384,               // 16 KB por chunk
                minimumAmountOfEntitiesPerChunk: 100,   // Mínimo de 100 entidades por chunk
                archetypeCapacity: 2,                   // Capacidade de 2 arquétipos por chunk
                entityCapacity: 64)                     // Capacidade de 64 entidades por chunk
        );
        
        // Sistemas Individuais (para serem injetados nos grupos)
        services.AddSingleton<NetworkToCommandSystem>();
        services.AddSingleton<EntitySystem>();
        services.AddSingleton<MovementValidationSystem>();
        services.AddSingleton<ServerProcessMovementSystem>();
        services.AddSingleton<ChatSystem>();
        
        // Grupos de Sistemas (como Singletons)
        services.AddSingleton(provider => new PhysicsSystemGroup(
            [
                // 1. Receber comandos da rede
                provider.GetRequiredService<NetworkToCommandSystem>(),
                // 2. Validar movimento
                provider.GetRequiredService<MovementValidationSystem>(),
                // 3. Processar movimento
                provider.GetRequiredService<ServerProcessMovementSystem>()
            ]
        ));
        
        services.AddSingleton(provider => new ProcessSystemGroup(
            [
                // 1. Gerenciar entidades de jogadores
                provider.GetRequiredService<EntitySystem>(),
                // 2. Gerenciar chat do servidor
                provider.GetRequiredService<ChatSystem>()
            ]
        ));
        
        // Registrar o ECS Runner que vai executar os grupos de sistemas
        services.AddSingleton<EcsRunner>(provider => new EcsRunner(
            provider.GetRequiredService<ILogger<EcsRunner>>(),
            provider.GetRequiredService<PhysicsSystemGroup>(),
            provider.GetRequiredService<ProcessSystemGroup>()
        ));
    }
}