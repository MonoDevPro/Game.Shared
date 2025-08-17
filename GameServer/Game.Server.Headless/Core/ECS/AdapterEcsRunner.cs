using Arch.Core;
using Game.Core.Common.Enums;
using Game.Server.Headless.Core.ECS.Game.Components;
using Microsoft.Extensions.Logging;
using Shared.ECS;
using Shared.ECS.Groups;

namespace Game.Server.Headless.Core.ECS;

public sealed class AdapterEcsRunner(
    ILogger<EcsRunner> logger, 
    World world,
    NetworkReceiveGroup networkReceiveGroup, 
    PhysicsSystemGroup physicsGroup, 
    ProcessSystemGroup processGroup, 
    NetworkSendGroup networkSendGroup) 
    : EcsRunner(logger, networkReceiveGroup, physicsGroup, processGroup, networkSendGroup)
{
    private readonly ILogger<EcsRunner> _logger = logger;

    public override void Initialize()
    {
        _logger.LogInformation("[Arch ECS] Inicializando AdapterEcsRunner...");
        // Chama a inicialização da classe base
        base.Initialize();
        _logger.LogInformation("[Arch ECS] AdapterEcsRunner inicializado.");
    }

}

