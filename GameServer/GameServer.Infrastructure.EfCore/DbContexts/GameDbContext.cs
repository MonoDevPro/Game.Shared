using System.Reflection;
using Game.Core.Entities.Account;
using Game.Core.Entities.Character;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.EfCore.DbContexts;

public class GameDbContext(DbContextOptions<GameDbContext> options)
    : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<CharacterEntity> Characters => Set<CharacterEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("gameserver");
        
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        base.OnModelCreating(builder);
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder conventions)
    {
        // Remover convenções que interferem com configurações específicas
        base.ConfigureConventions(conventions);
    }

}