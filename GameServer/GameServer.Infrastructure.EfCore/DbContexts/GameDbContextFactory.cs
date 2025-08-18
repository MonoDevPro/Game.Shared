using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameServer.Infrastructure.EfCore.DbContexts;

public class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite("Data Source=game_database.db", sqlite =>
            {
                sqlite.MigrationsAssembly(Assembly.GetExecutingAssembly());
            }).Options;

        return new GameDbContext(options);
    }
}