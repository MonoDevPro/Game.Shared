using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Hosted;

public class ApplyMigrationsHosted(
    IServiceProvider provider,
    ILogger<ApplyMigrationsHosted> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        
        // Descobre todos os DbContexts registrados no DI
        var dbContextTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(DbContext).IsAssignableFrom(t) && t is { IsAbstract: false, IsGenericType: false })
            .ToArray();

        var contexts = dbContextTypes
            .Select(t => scope.ServiceProvider.GetService(t) as DbContext)
            .Where(c => c != null)
            .ToArray();
        
        if (contexts.Length == 0)
        {
            logger.LogWarning("No DbContext instances found. Skipping database migration.");
            return;
        }
        
        logger.LogInformation("Found {Count} DbContext instances. Initialising databases...", contexts.Length);
        
        foreach (var context in contexts)
            try
            {
                await context!.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogInformation("Database for {DbContext} migrated successfully.", context.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initialising the database with {SeederType}", context!.GetType().Name);
                throw;
            }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Nothing to do on stop
        return Task.CompletedTask;
    }
}