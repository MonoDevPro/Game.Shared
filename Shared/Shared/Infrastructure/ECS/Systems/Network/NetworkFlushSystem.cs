using Shared.Infrastructure.Network;

namespace Shared.Infrastructure.ECS.Systems.Network;

/// <summary>
/// Sistema dedicado a enviar todos os pacotes de rede enfileirados.
/// Deve ser o último a ser executado no loop.
/// </summary>
public partial class NetworkFlushSystem(Arch.Core.World world, NetworkManager networkManager) : BaseSystem<Arch.Core.World, float>(world)
{
    public override void Update(in float t)
    {
        networkManager.Sender.FlushAllBuffers();
    }
}