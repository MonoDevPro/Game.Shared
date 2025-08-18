using Game.Core.Common.ValueObjetcs;

namespace Game.Server.Headless.Core.ECS.Game.Components.Intents;

/// <summary>
/// Intenção do cliente para mover em uma direção discreta (grid delta).
/// </summary>
public struct MoveIntent { public MapPosition Direction; }