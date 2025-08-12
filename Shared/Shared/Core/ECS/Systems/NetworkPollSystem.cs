using Shared.Core.Network;

namespace Shared.Core.ECS.Systems;

/// <summary>
/// Sistema dedicado a sondar a rede por novos pacotes.
/// Deve ser o primeiro a ser executado no loop.
/// </summary>
public partial class NetworkPollSystem(Arch.Core.World world, NetworkManager networkManager) : BaseSystem<Arch.Core.World, float>(world)
{
    public override void Update(in float t)
    {
        networkManager.PollEvents();
    }
}