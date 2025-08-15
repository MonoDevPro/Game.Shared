using Game.Core.Entities.Common.ValueObjetcs;

namespace Game.Core.ECS.Components;

/// <summary>
/// Armazena a posição LÓGICA da entidade no grid (em coordenadas de tile).
/// Esta é a fonte da verdade no servidor.
/// </summary>
public struct MapPositionComponent { public MapPosition Value; }