using Game.Core.Common.ValueObjetcs;

namespace Game.Server.Headless.Core.ECS.Game.Components.Commands;

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public MapPosition Direction; }