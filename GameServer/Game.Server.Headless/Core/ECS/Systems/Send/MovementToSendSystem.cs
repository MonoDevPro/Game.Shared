using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Commands;
using Game.Core.ECS.Components.Tags;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Transport;

namespace Game.Server.Headless.Core.ECS.Systems.Send;

public partial class MovementToSendSystem(World world, NetworkSender sender, ILogger<MovementToSendSystem> logger) 
    : BaseSystem<World, float>(world)
{
    
}