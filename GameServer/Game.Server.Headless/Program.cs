using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Game.Server.Headless;

public class Program
{
    public static void Main(string[] args)
    {
        // 1. Criar a fonte do token de cancelamento.
        var cts = new CancellationTokenSource();
        
        // 2. Coleção de serviços
        var services = new ServiceCollection();
        
        // 3. Configurar os serviços necessários no servidor
        services.ConfigureServices();
        
        // 4. Buildar o provedor de serviços
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Iniciando o servidor...");
        
        // 5. Configurar o manipulador de encerramento (Ctrl+C).
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            // Impede que a aplicação termine imediatamente.
            eventArgs.Cancel = true; 
            
            // Inicia o processo de cancelamento.
            cts.Cancel(); 
        };
        
        // 6. Obter o loop do servidor
        var serverLoop = serviceProvider.GetRequiredService<ServerLoop>();
        try
        {
            // 6. Executar o loop do servidor
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
            // 7. Limpar os serviços
            serviceProvider.Dispose();
            cts.Dispose();
        }
    }
}