using LiteNetLib;

namespace GameClient.Features.Player.Events;

/// <summary>
/// Evento que é disparado quando um jogador é removido do jogo.
/// Este evento é utilizado para notificar outros sistemas que o jogador não está mais ativo.
/// </summary>
/// <remarks>
/// Este evento pode ser utilizado para limpar recursos, atualizar a UI ou realizar outras ações
/// necessárias quando um jogador é removido.
/// </remarks>
public struct LocalPlayerDespawnedEvent { }

/// <summary>
/// Evento que indica que o jogador local foi spawnado no jogo.
/// Utilizado para desacoplar o sistema de spawn do jogador para a UI e
/// outros sistemas que precisam saber quando o jogador está pronto para interagir.
/// </summary>
public struct LocalPlayerSpawnedEvent { }