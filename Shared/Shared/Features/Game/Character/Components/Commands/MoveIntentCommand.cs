using Shared.Core.Common.Math;

namespace Shared.Features.Game.Character.Components.Commands;

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public GridVector Direction; }