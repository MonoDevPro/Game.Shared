namespace Game.Server.Headless.Core.ECS.Components;

/// <summary>
/// Componente exclusivo do servidor que armazena o último ID de sequência de input
/// processado para uma entidade de jogador. Usado para prevenção de spam.
/// </summary>
public struct ClientInputStateComponent
{
    public uint LastProcessedSequenceId;
}