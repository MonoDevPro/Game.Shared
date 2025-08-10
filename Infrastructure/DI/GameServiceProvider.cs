using System;
using Arch.Core;
using GameClient.Infrastructure.ECS.Systems.Network.Receive;
using GameClient.Infrastructure.ECS.Systems.Network.Send;
using GameClient.Infrastructure.ECS.Systems.Physics;
using GameClient.Infrastructure.ECS.Systems.Process;
using GameClient.Infrastructure.Logger;
using GameClient.Infrastructure.Network;
using Godot;
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
        services.AddLogging(configure => 
        {
            configure.ClearProviders(); // Opcional, mas bom para garantir que só o seu logger será usado
            configure.AddGodotLogger(); // <-- SUBSTITUA AddConsole() POR ISTO
        });
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
        services.AddSingleton<MovementUpdateSystem>();
        services.AddSingleton<LocalInputSystem>();
        services.AddSingleton<MovementStartSystem>();
        services.AddSingleton<MovementToSendSystem>();
        services.AddSingleton<MovementProcessSystem>();
        services.AddSingleton(provider => new PhysicsSystemGroup(
        [
            
            provider.GetRequiredService<MovementUpdateSystem>(),
            provider.GetRequiredService<LocalInputSystem>(),
            provider.GetRequiredService<MovementStartSystem>(),
            provider.GetRequiredService<MovementToSendSystem>(),
            provider.GetRequiredService<MovementProcessSystem>(),
        ]));
        
        // Sistemas de Processamento Geral, por enquanto vazio.
        
        services.AddSingleton<AnimationSystem>();
        services.AddSingleton<PlayerViewSystem>(provider => new PlayerViewSystem(provider.GetRequiredService<World>(), provider.GetRequiredService<Node>()
            .GetNode<Node>("/root/ClientBootstrap/PlayerView")));
        services.AddSingleton(provider => new ProcessSystemGroup(
        [
            provider.GetRequiredService<EntitySystem>(),
            provider.GetRequiredService<PlayerViewSystem>(),
            provider.GetRequiredService<AnimationSystem>(),
        ]));
        
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