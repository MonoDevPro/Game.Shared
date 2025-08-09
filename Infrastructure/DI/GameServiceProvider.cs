using System;
using Arch.Core;
using GameClient.Infrastructure.ECS.Systems.Network;
using GameClient.Infrastructure.ECS.Systems.Physics;
using GameClient.Infrastructure.ECS.Systems.Process;
using GameClient.Infrastructure.Network;
using Godot;
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

// ... (outros usings que você possa precisar)

namespace GameClient.Infrastructure.DI;

public partial class GameServiceProvider : Node
{
    // A instância estática para acesso global fácil e seguro
    public static GameServiceProvider Instance { get; private set; }

    public IServiceProvider Services { get; private set; }

    public override void _Ready()
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
        // 1. Serviços Base (Logging, Nós Godot, Mundo ECS, Mapa)
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<Node>(provider => Engine.GetMainLoop() is SceneTree tree ? tree.Root : throw new InvalidOperationException("SceneTree not found"));
        services.AddSingleton<World>(_ => World.Create());
        services.AddSingleton(new GameMap(100, 100));

        // 2. Serviços de Rede
        services.AddSingleton<INetLogger, LiteNetLibLogger>();
        services.AddSingleton<EventBasedNetListener>();
        services.AddSingleton(provider => new NetManager(provider.GetRequiredService<EventBasedNetListener>()));
        services.AddSingleton(provider => new NetPacketProcessor(NetworkConfigurations.MaxStringLength));
        services.AddSingleton<NetworkSender>();
        services.AddSingleton<NetworkReceiver>();
        services.AddSingleton<PeerRepository>();
        services.AddSingleton<NetworkManager, ClientNetwork>(); 

        // 3. Sistemas Individuais (Lógica e Visuais)
        // Lógica Pura
        services.AddSingleton<EntitySystem>(); // Renomeado a partir de EntitySystem.cs
        services.AddSingleton<NetworkToEntitySystem>();
        services.AddSingleton<NetworkToChatSystem>();
        services.AddSingleton<NetworkToMovementSystem>(); // O novo MovementUpdateSystem
        services.AddSingleton<LocalInputSystem>();
        services.AddSingleton<SendInputToServerSystem>();
        // Lógica Visual
        services.AddSingleton<PlayerViewSystem>(provider => new PlayerViewSystem(provider.GetRequiredService<World>(), provider.GetRequiredService<Node>()
            .GetNode<Node>("../PlayerView")));
        services.AddSingleton<MovementSystem>();
        services.AddSingleton<AnimationSystem>();

        // 4. Grupos de Sistemas (Definindo a Ordem de Execução)
        
        // --- GRUPO DE RECEPÇÃO DE REDE ---
        // Lógica de recepção de pacotes e comandos.
        services.AddSingleton(provider => new NetworkReceiveGroup(
            [
                provider.GetRequiredService<NetworkPollSystem>(),
                provider.GetRequiredService<NetworkToEntitySystem>(),
                provider.GetRequiredService<NetworkToChatSystem>(),
                provider.GetRequiredService<NetworkToMovementSystem>(),
            ]
        ));
        
        // --- GRUPO DE ENVIO DE REDE ---
        // Lógica de envio de pacotes e comandos.
        services.AddSingleton(provider => new NetworkSendGroup(
            [
                provider.GetRequiredService<SendInputToServerSystem>(),
                provider.GetRequiredService<NetworkFlushSystem>(),
            ]
        ));
        
        
        // --- GRUPO DE FÍSICA ---
        // Lógica sensível ao tempo: input e movimento visual.
        services.AddSingleton(provider => new PhysicsSystemGroup(
            [
                provider.GetRequiredService<LocalInputSystem>(),
                provider.GetRequiredService<MovementSystem>(),
            ]
        ));
        
        // --- GRUPO DE PROCESSO ---
        // Lógica de gestão de cena e UI.
        services.AddSingleton(provider => new ProcessSystemGroup(
            [
                provider.GetRequiredService<PlayerViewSystem>(),
                provider.GetRequiredService<AnimationSystem>(),
            ]
        ));
        
        // Registrar o ECS Runner que vai executar os grupos de sistemas
        services.AddSingleton<EcsRunner>(provider => new EcsRunner(
            provider.GetRequiredService<ILogger<EcsRunner>>(),
            provider.GetRequiredService<NetworkReceiveGroup>(),
            provider.GetRequiredService<PhysicsSystemGroup>(),
            provider.GetRequiredService<ProcessSystemGroup>(),
            provider.GetRequiredService<NetworkSendGroup>()
        ));  
    }
}