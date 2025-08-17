using Game.Core.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components.Commands;

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public MapPosition Direction; }