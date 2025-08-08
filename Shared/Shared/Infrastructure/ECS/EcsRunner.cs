using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.ECS;

/// <summary>
/// Orquestrador de sistemas ECS puro, sem dependências da Godot.
/// Gerencia o ciclo de vida e a execução de grupos de sistemas.
/// </summary>
public class EcsRunner(
    ILogger<EcsRunner> logger,
    ISystem<float> physicsGroup,
    ISystem<float> processGroup)
    : ISystem<float>
{
    public void Initialize()
    {
        physicsGroup.Initialize();
        processGroup.Initialize();
        logger.LogInformation("[Arch ECS] EcsRunner inicializado.");
    }

    public void BeforeUpdate(in float t)
    {
        physicsGroup.BeforeUpdate(in t);
        processGroup.BeforeUpdate(in t);
    }

    public void Update(in float t)
    {
        physicsGroup.Update(in t);
        processGroup.Update(in t);
    }

    public void AfterUpdate(in float t)
    {
        physicsGroup.AfterUpdate(in t);
        processGroup.AfterUpdate(in t);
    }
    
    public void UpdatePhysics(in float t)
    {
        physicsGroup.BeforeUpdate(in t);
        physicsGroup.Update(in t);
        physicsGroup.AfterUpdate(in t);
    }
    
    public void UpdateProcess(in float t)
    {
        processGroup.BeforeUpdate(in t);
        processGroup.Update(in t);
        processGroup.AfterUpdate(in t);
    }

    public void Dispose()
    {
        physicsGroup.Dispose();
        processGroup.Dispose();
        // O World é gerenciado pelo contêiner DI, então não o descartamos aqui.
        logger.LogInformation("[Arch ECS] EcsRunner finalizado e recursos liberados.");
        GC.SuppressFinalize(this);
    }
}