using System.Collections.Concurrent;
using Game.Core.Entities.Account;
using Game.Core.Entities.Common.Rules;
using GameServer.Infrastructure.EfCore.Hasher;
using GameServer.Infrastructure.EfCore.Repositories;

namespace Game.Server.Headless.Infrastructure.Repositories;

public class AccountRepository(IRepositoryCompose<AccountEntity> compose, IPasswordHasherService hasher)
{
    public async Task<bool> UsernameExists(string username) 
        => await compose.ReaderRepository.ExistsAsync(a => a.Username == username);
    public async Task<bool> EmailExists(string email) 
        => await compose.ReaderRepository.ExistsAsync(a => a.Email == email);

    public async Task<AccountEntity> Create(string username, string email, string password)
    {
        try
        {
            if (!UsernameRule.TryValidate(username, out var errorMessage))
                throw new ArgumentException(errorMessage ?? "Username is invalid.", nameof(username));
            if (!EmailRule.TryValidate(email, out errorMessage))
                throw new ArgumentException(errorMessage ?? "Email is invalid.", nameof(email));
            if (!PasswordRule.TryValidate(password, out errorMessage))
                throw new ArgumentException(errorMessage ?? "Password is invalid.", nameof(password));
            
            var hashedPassword = hasher.HashPassword(password);
            
            var account = new AccountEntity(username, email, hashedPassword);
            
            if (await UsernameExists(username))
                throw new ArgumentException($"Username '{username}' is already taken.", nameof(username));
            if (await EmailExists(email))
                throw new ArgumentException($"Email '{email}' is already registered.", nameof(email));
            
            await compose.WritterRepository.AddAsync(account);
            var saved = await compose.WritterRepository.SaveChangesAsync();
            if (!saved)
                throw new InvalidOperationException("Failed to create account.");
            return account;
            
        } catch (ArgumentException ex)
        {
            throw new ArgumentException(ex.Message, ex.ParamName);
        }
    }

    public async Task<AccountEntity?> GetByUsername(string username)
        => await compose.ReaderRepository.QuerySingleAsync(a => a.Username == username, a => a);

    public async Task<AccountEntity?> GetById(int id) 
        => await compose.ReaderRepository.QuerySingleAsync(a => a.Id == id, a => a);
}
