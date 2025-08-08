using System;
using Arch.Core;
using Game.Shared.Client.Infrastructure.ECS;
using Game.Shared.Client.Infrastructure.ECS.Systems;
using Game.Shared.Client.Infrastructure.Network;
using Game.Shared.Client.Infrastructure.Spawners;
using Game.Shared.Client.Presentation.UI.Chat;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS;
using Shared.Infrastructure.ECS.Groups;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;
using Shared.Infrastructure.World;

// Adicione os usings para todos os seus sistemas e serviços

namespace Game.Shared.Client.Infrastructure.Bootstrap;

public partial class GameServiceProvider : Node
{
    // A instância estática para acesso global fácil e seguro
    public static GameServiceProvider Instance { get; private set; }

    public IServiceProvider Services { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            // Garante que é um singleton
            QueueFree();
            return;
        }
        Instance = this;

        // Configura e constrói o contentor de serviços
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Logging
        // (Nota: o logger de consola pode não funcionar perfeitamente no editor Godot,
        // mas funcionará no executável exportado. Pode-se criar um ILoggerProvider customizado para o output da Godot)
        services.AddLogging(configure => configure.AddConsole());
        
        // Add Godot Tree as Singleton
        services.AddSingleton<Node>(provider => 
            Engine.GetMainLoop() is SceneTree tree 
                ? tree.Root 
                : throw new InvalidOperationException("SceneTree not found"));

        // 2. Singletons Essenciais do 'Shared'
        // Registrar o World do ECS
        services.AddSingleton<World>(_ =>
                World.Create(
                    chunkSizeInBytes: 16_384,               // 16 KB por chunk
                    minimumAmountOfEntitiesPerChunk: 100,   // Mínimo de 100 entidades por chunk
                    archetypeCapacity: 2,                   // Capacidade de 2 arquétipos por chunk
                    entityCapacity: 64)                     // Capacidade de 64 entidades por chunk
        );
        services.AddSingleton(new GameMap(100, 100)); // O cliente também pode ter uma cópia do mapa

        // 3. Serviços de Rede
        services.AddSingleton<INetLogger, LiteNetLibLogger>(); // A nossa ponte de log
        services.AddSingleton<EventBasedNetListener>();
        services.AddSingleton(provider => new NetManager(provider.GetRequiredService<EventBasedNetListener>()));
        services.AddSingleton<NetPacketProcessor>();
        services.AddSingleton<NetworkSender>();
        services.AddSingleton<NetworkReceiver>();
        services.AddSingleton<PeerRepository>();
        // Renomeie a sua classe NetworkManager do cliente para não haver conflito
        // Ex: ClientNetworkManager
        services.AddSingleton<NetworkManager, ClientNetwork>(); 

        // Grupos de Sistemas (como Singletons)
        services.AddSingleton(provider => new PhysicsSystemGroup(
            [
                provider.GetRequiredService<NetworkToCommandSystem>(),
                provider.GetRequiredService<MovementUpdateSystem>(),
                provider.GetRequiredService<LocalInputSystem>(),
                provider.GetRequiredService<ClientProcessMovementSystem>(),
                provider.GetRequiredService<SendInputSystem>(),
            ]
        ));
        
        services.AddSingleton(provider => new ProcessSystemGroup(
            [
                // 1. Gerenciar entidades de jogadores
                provider.GetRequiredService<AnimationSystem>(),
                // 2. Gerenciar chat do servidor
                provider.GetRequiredService<ClientChatSystem>(),
            ]
        ));

        // 5. Sistemas Individuais do Cliente
        services.AddSingleton<LocalInputSystem>();
        services.AddSingleton<SendInputSystem>();
        services.AddSingleton<MovementUpdateSystem>();
        services.AddSingleton<AnimationSystem>();
        services.AddSingleton<ClientChatSystem>();
        
        services.AddSingleton<PlayerSpawner>(provider =>
        {
            var playerSpawner = provider
                .GetRequiredService<Node>()
                .GetNode<ClientPlayerSpawner>(nameof(ClientPlayerSpawner));
            return playerSpawner;
        });
        
        services.AddSingleton<ChatUI>(provider =>
        {
            var chatUI = provider
                .GetRequiredService<Node>()
                .GetNode<ChatUI>("/GameUI/ChatUI");
            return chatUI;
        });
        
        // Registrar o ECS Runner que vai executar os grupos de sistemas
        services.AddSingleton<EcsRunner>(provider => new EcsRunner(
            provider.GetRequiredService<ILogger<EcsRunner>>(),
            provider.GetRequiredService<PhysicsSystemGroup>(),
            provider.GetRequiredService<ProcessSystemGroup>()
        ));
    }
}