using System.Linq;
using Architect.Objects.Categories;
using Architect.Objects.Placeable;
using BepInEx;
using BepInEx.Logging;

namespace EnemyRando;

[BepInAutoPlugin(id: "me.cometcake575.enemyrando")]
[BepInDependency("com.cometcake575.architect")]
public partial class EnemyRandoPlugin : BaseUnityPlugin
{
    internal static EnemyRandoPlugin Instance = null!;
    internal new static ManualLogSource Logger = null!;
    
    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        
        // Put your initialization logic here
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        
        Settings.Init(Config);
        Randomiser.Init();

        foreach (var enemy in Categories.Enemies.GetObjects().OfType<PlaceableObject>()) 
            enemy.ShouldLoad = true;
    }
}