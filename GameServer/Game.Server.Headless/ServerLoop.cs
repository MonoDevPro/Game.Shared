using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.ECS;
using Shared.Network;

namespace Game.Server.Headless;

/// <summary>
/// Encapsula o loop principal do servidor, gerindo o tick rate e a
/// execução ordenada dos sistemas de jogo e de rede.
/// </summary>
public class ServerLoop(
    ILogger<ServerLoop> logger,
    NetworkManager networkManager,
    EcsRunner ecsRunner)
{
    private const int TICK_RATE_HZ = 30;
    private const float DELTA_TIME_S = 1.0f / TICK_RATE_HZ;
    private const int MS_PER_TICK = 1000 / TICK_RATE_HZ;

    /// <summary>
    /// Inicia e executa o loop principal do servidor.
    /// Este método irá bloquear o thread de execução.
    /// </summary>
    public void Run(CancellationToken cancellationToken)
    {
        logger.LogInformation("A iniciar os sistemas do servidor...");
        ecsRunner.Initialize();

        logger.LogInformation("A iniciar o gestor de rede...");
        networkManager.Start();
        
        logger.LogInformation("Servidor iniciado. Tick Rate: {TickRate} Hz", TICK_RATE_HZ);
        logger.LogInformation("Pressione Ctrl+C para encerrar.");

        var watch = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested)
        {
            watch.Restart();
            
            // Executa os grupos de sistemas na ordem correta
            ecsRunner.BeforeUpdate(DELTA_TIME_S);
            ecsRunner.Update(DELTA_TIME_S);
            ecsRunner.AfterUpdate(DELTA_TIME_S);
            
            // Envia todos os pacotes de rede enfileirados
            //networkManager.Sender.FlushAllBuffers();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            // Garante um tick rate estável
            if (elapsedMs < MS_PER_TICK)
                Thread.Sleep((int)(MS_PER_TICK - elapsedMs));
        }

        // Código de limpeza (será executado se o loop for quebrado)
        ecsRunner.Dispose();
        networkManager.Stop();
    }
}