using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1) Cancellation via Ctrl+C
        using var cts = new CancellationTokenSource();

        // 2) Build Generic Host so HostedServices (DB worker, migrations) actually start
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.ConfigureServices())
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Iniciando o servidor...");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // don't terminate immediately
            cts.Cancel();
        };

        // 3) Start HostedServices (DatabaseWorker, migrations) and then run the server loop
        await host.StartAsync(cts.Token);

        var serverLoop = host.Services.GetRequiredService<ServerLoop>();
        try
        {
            serverLoop.Run(cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Servidor encerrado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no servidor.");
        }
        finally
        {
            // 4) Stop HostedServices gracefully
            try { await host.StopAsync(TimeSpan.FromSeconds(10)); } catch { /* ignore */ }
        }
    }
}