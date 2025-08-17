using Game.Core.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components;

/// <summary>
/// Armazena a posição LÓGICA da entidade no grid (em coordenadas de tile).
/// Esta é a fonte da verdade no servidor.
/// </summary>
public struct MapPositionComponent { public MapPosition Value; }