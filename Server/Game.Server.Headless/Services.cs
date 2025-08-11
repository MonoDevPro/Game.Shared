using Arch.Core;
using Game.Server.Headless.Infrastructure.ECS.Systems.Network;
using Game.Server.Headless.Infrastructure.ECS.Systems.Network.Receive;
using Game.Server.Headless.Infrastructure.ECS.Systems.Network.Send;
using Game.Server.Headless.Infrastructure.ECS.Systems.Process;
using Game.Server.Headless.Infrastructure.Network;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS;
using Shared.Infrastructure.ECS.Groups;
using Shared.Infrastructure.ECS.Systems;
using Shared.Infrastructure.ECS.Systems.Network;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Config;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;
using Shared.Infrastructure.WorldGame;

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
        services.AddSingleton(provider => new NetPacketProcessor(NetworkConfigurations.MaxStringLength));
        services.AddSingleton<NetworkSender>();
        services.AddSingleton<NetworkReceiver>();
        services.AddSingleton<PeerRepository>();
        services.AddSingleton<NetworkManager, ServerNetwork>(); 
        
        // Sistemas de Gestão de Entidades
        services.AddSingleton<EntitySystem>(); 
        
        // Sistemas de Rede - Recepção
        services.AddSingleton<NetworkPollSystem>();
        services.AddSingleton<NetworkToChatSystem>();
        services.AddSingleton<NetworkToEntitySystem>();
        services.AddSingleton<NetworkToMovementSystem>();
        services.AddSingleton(provider => new NetworkReceiveGroup(
        [
            provider.GetRequiredService<NetworkPollSystem>(),
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
        services.AddSingleton<MovementToSendSystem>();
        services.AddSingleton<MovementProcessSystem>();
        services.AddSingleton(provider => new PhysicsSystemGroup(
        [
            provider.GetRequiredService<MovementStartSystem>(),
            provider.GetRequiredService<MovementToSendSystem>(),
            provider.GetRequiredService<MovementProcessSystem>(),
        ]));
        
        // Sistemas de Processamento Geral
        services.AddSingleton(provider => new ProcessSystemGroup(
        [
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