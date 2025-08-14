using Shared.Core.Common.Math;

namespace Shared.Features.Game.Character.Components;

/// <summary>
/// Armazena a posição LÓGICA da entidade no grid (em coordenadas de tile).
/// Esta é a fonte da verdade no servidor.
/// </summary>
public struct GridPositionComponent { public GridVector Value; }