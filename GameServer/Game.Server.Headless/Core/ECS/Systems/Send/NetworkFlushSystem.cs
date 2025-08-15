using Arch.Core;
using Arch.System;
using Shared.Core.Network;

namespace Game.Server.Headless.Core.ECS.Systems.Send;

/// <summary>
/// Sistema dedicado a enviar todos os pacotes de rede enfileirados.
/// Deve ser o último a ser executado no loop.
/// </summary>
public partial class NetworkFlushSystem(World world, NetworkManager networkManager) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        networkManager.Sender.FlushAllBuffers();
    }
}