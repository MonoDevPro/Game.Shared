using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.ECS.Commands;

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public GridVector Direction; }