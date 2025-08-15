namespace GameClient.Core.Services;

public class SelectedCharacterService
{
    public int? SelectedCharacterId { get; set; }
    public void Clear() => SelectedCharacterId = null;
}
