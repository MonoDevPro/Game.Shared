namespace Game.Server.Headless.Core.ECS.Game.Components.Intents;

public struct ExitGameIntent
{
    public int PeerId; // Identificador do jogador que está saindo do jogo
    public int CharacterId; // Identificador do personagem que está sendo removido
    public string Reason; // Motivo da saída, se necessário (opcional)
}