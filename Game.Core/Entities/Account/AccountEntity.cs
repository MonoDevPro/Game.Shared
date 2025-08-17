using Game.Core.Common;
using Game.Core.Common.Enums;
using Game.Core.Common.Rules;
using Game.Core.Entities.Character;

namespace Game.Core.Entities.Account;

public class AccountEntity(string username, string email, string passwordHash) : BaseEntity
{
    public string Username { get; private set; } = username;

    public string Email { get; private set; } = email;

    public string PasswordHash { get; private set; } = passwordHash;

    // Characters collection
    private readonly List<Character.CharacterEntity> _characters = [];
    public IReadOnlyCollection<Character.CharacterEntity> Characters => _characters.AsReadOnly();

    public void AddCharacter(CharacterEntity characterEntity)
    {
        if (characterEntity == null) 
            throw new ArgumentNullException(nameof(characterEntity));
        
        if (_characters.Any(c => c.Name == characterEntity.Name))
            throw new InvalidOperationException($"Character with name '{characterEntity.Name}' already exists.");
        
        if (_characters.Count >= CharacterConstants.MaxCharacterCount)
            throw new InvalidOperationException("Maximum number of characters for this account reached (5).");
        
        if (characterEntity.AccountId != Id)
            throw new InvalidOperationException("Character does not belong to this account.");
        
        if (!CharacterNameRule.TryValidate(characterEntity.Name, out var errorMessage))
            throw new ArgumentException(errorMessage ?? "Character name is invalid.", nameof(characterEntity.Name));
        
        if (characterEntity.Vocation == VocationEnum.None)
            throw new ArgumentException("Character vocation cannot be None.", nameof(characterEntity.Vocation));

        if (characterEntity.Gender == GenderEnum.None)
            throw new ArgumentException("Character gender cannot be None.", nameof(characterEntity.Gender));
        
        if (characterEntity.Direction == DirectionEnum.None)
            throw new ArgumentException("Character direction cannot be None.", nameof(characterEntity.Direction));
        
        _characters.Add(characterEntity);
    }
}