using Arch.Core;
using Game.Server.Headless.Infrastructure.ECS.Systems.Physics;
using Game.Server.Headless.Infrastructure.ECS.Systems.Process;
using Game.Server.Headless.Infrastructure.Network;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS;
using Shared.Infrastructure.ECS.Groups;
using Shared.Infrastructure.ECS.Systems.Network;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Config;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;
using Shared.Infrastructure.World;

namespace Game.Server.Headless;

public static class Services
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // 1. Serviços Base
        services.AddLogging(configure => configure.AddConsole());
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
        services.AddSingleton(provider => new NetPacketProcessor(NetworkConfigurations.MaxStringLength));
        services.AddSingleton<NetworkSender>();
        services.AddSingleton<NetworkReceiver>();
        services.AddSingleton<PeerRepository>();
        services.AddSingleton<NetworkManager, ServerNetwork>(); 
        
        // 3. Sistemas Individuais e Serviços de Gestão
        services.AddSingleton<EntitySystem>(); 
        services.AddSingleton<MovementValidationSystem>();
        services.AddSingleton<MovementSystem>();
        services.AddSingleton<NetworkPollSystem>();
        services.AddSingleton<NetworkFlushSystem>();
        
        // 3. Sistemas Individuais e Serviços de Gestão
        services.AddSingleton<EntitySystem>(); 
        services.AddSingleton<MovementValidationSystem>();
        services.AddSingleton<MovementSystem>();
        services.AddSingleton<NetworkPollSystem>();
        services.AddSingleton<NetworkFlushSystem>();
        services.AddSingleton<NetworkToCommandSystem>();
        services.AddSingleton<NetworkToChatSystem>();
        
        // 4. Grupos de Sistemas (Definindo a Ordem de Execução)
        services.AddSingleton(provider => new NetworkReceiveGroup(
        [
            provider.GetRequiredService<NetworkPollSystem>(),
            provider.GetRequiredService<NetworkToCommandSystem>(),
        ]));
        
        services.AddSingleton(provider => new PhysicsSystemGroup(
        [
            provider.GetRequiredService<MovementValidationSystem>(),
            provider.GetRequiredService<MovementSystem>()
        ]));
        
        // O ProcessSystemGroup está agora vazio. Pode ser removido ou mantido para futuros sistemas.
        // Se não tiver mais nenhum sistema de processo, pode remover este grupo.
        services.AddSingleton(provider => new ProcessSystemGroup(
        [
            // Vazio por agora. Futuramente, um sistema de regeneração de mana iria aqui.
        ]));
        
        services.AddSingleton(provider => new NetworkSendGroup(
        [
            provider.GetRequiredService<NetworkFlushSystem>()
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