using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Input;
using Game.Shared.Shared.Infrastructure.Spawners;
using LiteNetLib;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

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
    
    public override void Update(in float t)
    {
        _spawner.NetworkManager.PollEvents();
        
        base.Update(in t);
    }
    
    /// <summary>
    /// Chamado quando uma atualização de estado (nova posição no grid) é recebida do servidor.
    /// </summary>
    private void OnStateSyncReceived(StateResponse packet, NetPeer peer)
    {
        // Encontra a entidade do personagem correspondente ao NetId do pacote.
        if (!_spawner.TryGetPlayerByNetId(packet.NetId, out var character))
            return;

        var entity = character.Entity;

        // Adiciona um comando à entidade com a nova posição do grid.
        // O GridMovementSystem irá processar este comando para iniciar a interpolação visual.
        // A lógica é a mesma tanto para o jogador local quanto para os remotos.
        World.Add(entity, new StateUpdateCommand
        {
            NetId = packet.NetId,
            NewGridPosition = packet.GridPosition
        });
    }

    public override void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        
        base.Dispose();
    }
}
