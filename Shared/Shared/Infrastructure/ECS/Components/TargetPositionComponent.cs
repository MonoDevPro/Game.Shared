using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.ECS.Components;

/// <summary>
/// (Apenas no servidor) Componente que armazena o alvo do movimento em pixels.
/// </summary>
public struct TargetPositionComponent { public WorldPosition Value; }