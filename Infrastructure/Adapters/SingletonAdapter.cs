using Godot;

namespace GameClient.Infrastructure.Adapters;

public static class SingletonAdapter
{
    public static T GetSingleton<T>() where T : Node
    {
        // Se o nome não for fornecido, usa o nome da classe (ex: "GameServiceProvider")
        var singleton = Engine.GetMainLoop() is SceneTree tree 
            // CORREÇÃO: Procura diretamente por um filho do root com o nome dado.
            ? tree.Root.GetNodeOrNull<T>(typeof(T).Name)
            : null;
        
        if (singleton != null)
            return singleton;
        
        GD.PrintErr($"[SingletonAdapter] Singleton de tipo '{typeof(T).Name}' com o nome '{typeof(T).Name}' não foi encontrado como filho de /root.");
        return null;
    }
}