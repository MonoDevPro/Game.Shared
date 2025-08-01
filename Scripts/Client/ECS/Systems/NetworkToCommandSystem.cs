using Arch.Core;
using Arch.System;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using LiteNetLib;

namespace Game.Shared.Scripts.Client.ECS.Systems;

// Renomeamos o sistema para refletir sua única responsabilidade
public partial class NetworkToCommandSystem : BaseSystem<World, float>
{
    private readonly List<IDisposable> _disposables = [];
    
    private readonly PlayerSpawner _spawner;

    public NetworkToCommandSystem(World world, PlayerSpawner spawner) : base(world) 
    {
        _spawner = spawner;
        // Corrigido para ouvir a mensagem correta
        _disposables.Add(_spawner.NetworkManager.Receiver.RegisterMessageHandler<StateResponse>(OnStateSyncReceived));
    }
    
    public override void Update(in float t) => _spawner.NetworkManager.PollEvents();

    private void OnStateSyncReceived(StateResponse packet, NetPeer peer)
    {
        // A sua busca pela entidade está perfeita.
        if (!_spawner.TryGetPlayerByNetId(packet.NetId, out var character))
            return;

        var entity = character.Entity; // Pegando a entidade ECS do script do personagem

        // Cenário 1: A atualização é para o nosso próprio jogador (Reconciliação)
        if (World.Has<PlayerControllerTag>(entity))
        {
            // Nós NÃO aplicamos a posição diretamente.
            // Em vez disso, adicionamos um comando com os dados do servidor.
            // Outro sistema (ReconciliationSystem) será responsável por comparar
            // a predição local com este estado autoritativo e fazer a correção.
            World.Add(entity, new AuthoritativeStateCommand 
            { 
                Position = packet.Position, 
                Velocity = packet.Velocity 
            });
        }
        // Cenário 2: A atualização é para um jogador remoto (Interpolação)
        else if (World.Has<RemoteProxyTag>(entity))
        {
            // Para outros jogadores, nós atualizamos os dados de interpolação.
            if (World.Has<InterpolationDataComponent>(entity))
            {
                ref var interpolation = ref World.Get<InterpolationDataComponent>(entity);
                ref var currentPos = ref World.Get<PositionComponent>(entity);

                // O ponto de partida é a posição atual. O alvo é a posição do pacote.
                interpolation.StartPosition = currentPos.Value;
                interpolation.TargetPosition = packet.Position;
                interpolation.TimeElapsed = 0f; // Reseta o timer da interpolação.
            }
            else
            {
                // Se for a primeira vez, apenas define a posição inicial.
                World.Add(entity, new InterpolationDataComponent
                {
                    StartPosition = packet.Position,
                    TargetPosition = packet.Position,
                    TimeElapsed = 0f
                });
            }
        }
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        
        base.Dispose();
    }
}
