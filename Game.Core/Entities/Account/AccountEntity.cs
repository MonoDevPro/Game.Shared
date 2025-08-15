using System.ComponentModel.DataAnnotations;
using Game.Core.Entities.Character;
using Game.Core.Entities.Common;
using Game.Core.Entities.Common.Enums;
using Game.Core.Entities.Common.Rules;

namespace Game.Core.Entities.Account;

public class AccountEntity : BaseEntity
{
    public string Username { get; private set; }
    
    public string Email { get; private set; }

    public string PasswordHash { get; private set; }
    
    // Characters collection
    private readonly List<Character.CharacterEntity> _characters = [];
    public IReadOnlyCollection<Character.CharacterEntity> Characters => _characters.AsReadOnly();
    
    public AccountEntity (string username, string email, string passwordHash)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
    }
    
    public void AddCharacter(Character.CharacterEntity characterEntity)
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