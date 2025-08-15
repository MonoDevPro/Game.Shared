using Arch.System;

namespace Game.Core.ECS.Groups;

/// <summary>
/// Define a ordem de execução para todos os sistemas relacionados com a rede.
/// A ordem é crucial: Receber Dados -> Processar Dados -> Enviar Dados.
/// </summary>
public class NetworkSendGroup(params ISystem<float>[] systems) : Group<float>("NetworkSendGroup", systems);