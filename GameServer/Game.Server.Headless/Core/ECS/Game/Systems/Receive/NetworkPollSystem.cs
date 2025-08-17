using Arch.Core;
using Arch.System;
using Shared.Network;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Receive;

/// <summary>
/// Sistema dedicado a sondar a rede por novos pacotes.
/// Deve ser o primeiro a ser executado no loop.
/// </summary>
public partial class NetworkPollSystem(World world, NetworkManager networkManager) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        networkManager.PollEvents();
    }
}