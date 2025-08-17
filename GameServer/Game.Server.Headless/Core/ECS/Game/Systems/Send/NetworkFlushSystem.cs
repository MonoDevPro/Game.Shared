using Arch.Core;
using Arch.System;
using Shared.Network;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Send;

/// <summary>
/// Sistema dedicado a enviar todos os pacotes de rede enfileirados.
/// Deve ser o último a ser executado no loop.
/// </summary>
public class NetworkFlushSystem(World world, NetworkManager networkManager) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        networkManager.Sender.FlushAllBuffers();
    }
}