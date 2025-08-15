using Shared.Core.Network;

namespace Shared.ECS;

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