using Microsoft.Extensions.Logging;
using Shared.ECS.Groups;

namespace Shared.ECS;

/// <summary>
/// Orquestrador de sistemas ECS puro, sem dependências da Godot.
/// Gerencia o ciclo de vida e a execução de grupos de sistemas.
/// </summary>
public class EcsRunner(
    ILogger<EcsRunner> logger,
    NetworkReceiveGroup networkReceiveGroup,
    PhysicsSystemGroup physicsGroup,
    ProcessSystemGroup processGroup,
    NetworkSendGroup networkSendGroup)
    : ISystem<float>
{
    public virtual void Initialize()
    {
        logger.LogInformation("[Arch ECS] Inicializando EcsRunner...");
        // Inicializa os grupos de sistemas
        networkReceiveGroup.Initialize();
        physicsGroup.Initialize();
        processGroup.Initialize();
        networkSendGroup.Initialize();
        logger.LogInformation("[Arch ECS] EcsRunner inicializado.");
    }

    public void BeforeUpdate(in float t)
    {
        // Executa a atualização antes de todos os grupos
        networkReceiveGroup.BeforeUpdate(in t);
        physicsGroup.BeforeUpdate(in t);
        processGroup.BeforeUpdate(in t);
        networkSendGroup.BeforeUpdate(in t);
    }

    public void Update(in float t)
    {
        // Executa a atualização dos grupos de sistemas
        networkReceiveGroup.Update(in t);
        physicsGroup.Update(in t);
        processGroup.Update(in t);
        networkSendGroup.Update(in t);
    }

    public void AfterUpdate(in float t)
    {
        // Executa a atualização após todos os grupos
        networkReceiveGroup.AfterUpdate(in t);
        physicsGroup.AfterUpdate(in t);
        processGroup.AfterUpdate(in t);
        networkSendGroup.AfterUpdate(in t);
    }
    
    public void Dispose()
    {
        logger.LogInformation("[Arch ECS] EcsRunner finalizando e liberando recursos...");
        // Libera os recursos dos grupos de sistemas
        networkReceiveGroup.Dispose();
        physicsGroup.Dispose();
        processGroup.Dispose();
        networkSendGroup.Dispose();
        // O World é gerenciado pelo contêiner DI, então não o descartamos aqui.
        logger.LogInformation("[Arch ECS] EcsRunner finalizado e recursos liberados.");
        GC.SuppressFinalize(this);
    }
}