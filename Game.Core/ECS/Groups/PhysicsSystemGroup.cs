using Arch.System;

namespace Game.Core.ECS.Groups;

/// <summary>
/// Define a ordem de execução para todos os sistemas relacionados com a física e a simulação de movimento.
/// A ordem é crucial: Receber Input -> Validar -> Processar Movimento.
/// </summary>
public class PhysicsSystemGroup(ISystem<float>[] systems) : Group<float>("PhysicsGroup", systems);