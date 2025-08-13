using Shared.Infrastructure.Math;

namespace Shared.Features.Player.Components.Commands;

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public GridVector Direction; }