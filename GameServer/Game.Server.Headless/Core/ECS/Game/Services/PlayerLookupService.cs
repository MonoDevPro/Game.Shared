using Arch.Core;
using Game.Server.Headless.Core.ECS.Game.Components;

namespace Game.Server.Headless.Core.ECS.Game.Services;

public class PlayerLookupService(World world)
{
    // Recebe o World, não cria nada.

    // Apenas lê o estado que já existe no World.
    public bool TryGetPlayerEntity(int netId, out Entity entity)
    {
        // 1. Crie uma variável LOCAL para receber o valor de dentro da lambda.
        Entity localFoundEntity = Entity.Null;
        bool wasFound = false;

        var query = new QueryDescription().WithAll<PlayerRegistryComponent>();
        world.Query(in query, (ref PlayerRegistryComponent registry) =>
        {
            // 2. Use a variável LOCAL aqui, dentro da lambda.
            wasFound = registry.PlayersByNetId.TryGetValue(key: netId, value: out localFoundEntity);
        });

        // 3. Fora da lambda, atribua o valor da variável local ao parâmetro 'out'.
        entity = localFoundEntity;

        return wasFound && world.IsAlive(entity);
    }
}