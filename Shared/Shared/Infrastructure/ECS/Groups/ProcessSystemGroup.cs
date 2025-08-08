namespace Shared.Infrastructure.ECS.Groups;

/// <summary>
/// Define a ordem de execução para sistemas de processo (lógica de jogo, chat, etc.).
/// </summary>
public class ProcessSystemGroup(ISystem<float>[] systems) : Group<float>("ProcessGroup", systems);
