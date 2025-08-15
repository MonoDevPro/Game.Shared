using Game.Core.Entities.Common.ValueObjetcs;

namespace Game.Core.ECS.Components.Commands;

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public MapPosition Direction; }