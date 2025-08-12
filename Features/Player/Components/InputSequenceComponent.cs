namespace GameClient.Features.Player.Components;

/// <summary>
/// Componente exclusivo do cliente que armazena o próximo ID de sequência
/// a ser enviado nos pacotes de input do jogador local.
/// </summary>
public struct InputSequenceComponent
{
    public uint NextId;
}