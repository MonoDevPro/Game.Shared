namespace GameClient.Core.ECS.Components.Tags;

/// <summary>
/// Marca uma entidade que representa outro jogador conectado (um "proxy").
/// Usado no cliente para aplicar interpolação de estado em vez de predição.
/// </summary>
public struct RemoteProxyTag { }