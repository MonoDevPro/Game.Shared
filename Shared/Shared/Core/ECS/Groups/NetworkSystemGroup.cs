namespace Shared.Core.ECS.Groups;

/// <summary>
/// Define a ordem de execução para todos os sistemas relacionados com a rede.
/// A ordem é crucial: Receber Dados -> Processar Dados -> Enviar Dados.
/// </summary>
public class NetworkReceiveGroup(params ISystem<float>[] systems) : Group<float>("NetworkReceiveGroup", systems);
public class NetworkSendGroup(params ISystem<float>[] systems) : Group<float>("NetworkSendGroup", systems);