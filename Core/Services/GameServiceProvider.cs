using System;
using Arch.Core;
using GameClient.Core.ECS.Systems;
using GameClient.Core.Logger;
using GameClient.Core.Networking;
using GameClient.Features.Game.Chat.Systems;
using GameClient.Features.Game.Player.Systems.Network;
using GameClient.Features.Game.Player.Systems.Physics;
using GameClient.Features.MainMenu.NetworkStatus;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Core.Common.Constants;
using Shared.Core.ECS;
using Shared.Core.ECS.Groups;
using Shared.Core.ECS.Systems;
using Shared.Core.Network;
using Shared.Core.Network.Repository;
using Shared.Core.Network.Transport;
using Shared.Features.Game.Character.Systems;
using Shared.Infrastructure.WorldGame;
// ... (outros usings que você possa precisar)

namespace GameClient.Core.Services;

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
            configure.SetMinimumLevel(LogLevel.Debug);
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
        // Sistema de Eventos do ECS
        services.AddSingleton<EventHandler>();
        
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
        services.AddSingleton<ReconciliationSystem>();
        services.AddSingleton<RemoteMoveSystem>();
        services.AddSingleton<PlayerInputSystem>();
        services.AddSingleton<AttackProcessSystem>();
        services.AddSingleton<MovementStartSystem>();
        services.AddSingleton<MovementToSendSystem>();
        services.AddSingleton<MovementProcessSystem>();
        services.AddSingleton(provider => new PhysicsSystemGroup(
        [
            provider.GetRequiredService<ReconciliationSystem>(),
            provider.GetRequiredService<RemoteMoveSystem>(),
            provider.GetRequiredService<PlayerInputSystem>(),
            provider.GetRequiredService<AttackProcessSystem>(),
            provider.GetRequiredService<MovementStartSystem>(),
            provider.GetRequiredService<MovementToSendSystem>(),
            provider.GetRequiredService<MovementProcessSystem>(),
        ]));
        
        // Sistemas de Processamento Geral, por enquanto vazio.
        
        services.AddSingleton<AnimationSystem>();
        services.AddSingleton<VisualUpdateSystem>();
        services.AddSingleton<PlayerSpawnSystem>(provider => new PlayerSpawnSystem(provider.GetRequiredService<World>(), provider.GetRequiredService<Node>()
            .GetNode<Node>("/root/ClientBootstrap/PlayerView")));
        services.AddSingleton(provider => new ProcessSystemGroup(
        [
            //provider.GetRequiredService<EntitySystem>(),
            provider.GetRequiredService<PlayerSpawnSystem>(),
            provider.GetRequiredService<AnimationSystem>(),
            provider.GetRequiredService<VisualUpdateSystem>(), // <-- ADICIONE O SISTEMA AO GRUPO DE EXECUÇÃO
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