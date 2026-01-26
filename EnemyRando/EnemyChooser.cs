using System;
using System.Linq;
using Architect.Objects.Categories;
using Architect.Objects.Placeable;
using HutongGames.PlayMaker.Actions;
using Encoding = System.Text.Encoding;
using Random = UnityEngine.Random;

namespace EnemyRando;

public static class EnemyChooser
{
    private static bool _setupObjects;
    
    private static PlaceableObject[]? _all;
    private static PlaceableObject[]? _bosses;
    private static PlaceableObject[]? _enemies;

    private static readonly string[] IgnoredEnemies =
    [
        "surface_scuttler",
        "slab_jailer",
        "white_palace_fly",
        "garpid",
        "crystal_drifter",
        "leaf_glider",
        "snitchfly",
        "brushflit"
    ];

    // Anything that spawns enemies, either on death or in general
    private static readonly string[] SummonerEnemies =
    [
        "song_maestro",
        "broodmother",
        "gargant_gloom",
        "grove_pilgrim",
        "coral_big_jellyfish",
    ];

    private static void SetupObjects()
    {
        if (_setupObjects) return;
        _all ??= Categories.Enemies.GetObjects().Cast<PlaceableObject>()
            .Where(o =>
                o.Prefab.GetComponent<HealthManager>() &&
                !IgnoredEnemies.Contains(o.GetId())
                && o.GetId() == "gloomsac").ToArray();
        _enemies ??= _all.Where(o => 
            GetEnemyType(o.Prefab.gameObject.GetComponent<HealthManager>()) == EnemyType.Enemy).ToArray();
        _bosses ??= _all.Where(o => 
            GetEnemyType(o.Prefab.gameObject.GetComponent<HealthManager>()) == EnemyType.Boss).ToArray();
        _setupObjects = true;
    }
    
    public static (PlaceableObject, bool, bool) GetRandomEnemy(HealthManager source, Settings.RandoType type)
    {
        SetupObjects();

        var pool = (type switch
        {
            Settings.RandoType.Enemy => _enemies,
            Settings.RandoType.Boss => _bosses,
            _ => _all
        })!;
        var count = pool.Length;


        int seed;
        
        var consistency = Settings.Consistency.Value;
        switch (consistency)
        {
            case Settings.ConsistencyType.EnemyType:
            {
                var ede = source.enemyDeathEffects;
                if (ede)
                {
                    var jr = ede.journalRecord;
                    seed = GetCode(jr ? jr.displayName : source.gameObject.scene.name);
                }
                else seed = GetCode(source.gameObject.scene.name);
                Random.InitState(seed + Settings.Seed.Value);
                break;
            }
            case Settings.ConsistencyType.Individual:
                var conc = source.name + source.gameObject.scene.name;
                if (source.transform.parent) conc += source.transform.parent.name;
                seed = GetCode(conc);
                Random.InitState(seed + Settings.Seed.Value);
                break;
            case Settings.ConsistencyType.Room:
                seed = GetCode(source.gameObject.scene.name);
                Random.InitState(seed + Settings.Seed.Value);
                break;
            case Settings.ConsistencyType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var o = pool[Random.Range(0, count)];
        if (source.name.Contains("Slab Fly Small Fresh") || 
            source.name.Contains("Aspid Hatchling") ||
            source.name.Contains("Gloomfly") ||
            source.name.Contains("Song Automaton Tiny") ||
            source.name.Contains("Coral Swimmer Small"))
        {
            while (SummonerEnemies.Contains(o.GetId())) o = pool[Random.Range(0, count)];
        }

        bool blackThreaded;
        bool plasmified;
        if (PlayerData.instance.GetBool("blackThreadWorld"))
        {
            blackThreaded = Random.value <= Settings.BlackThreadChanceAct3.Value;
            plasmified = Random.value <= Settings.PlasmifiedChanceAct3.Value;
        }
        else
        {
            blackThreaded = Random.value <= Settings.BlackThreadChance.Value;
            plasmified = Random.value <= Settings.PlasmifiedChance.Value;
        }

        return (o, blackThreaded, plasmified);
    }

    private static int GetCode(string data)
    {
        var count = 0;
        foreach (var b in Encoding.Unicode.GetBytes(data))
        {
            count += b;
        }

        return count;
    }

    public enum EnemyType
    {
        Enemy,
        Boss,
        Misc
    }

    public static Settings.RandoType GetRandoType(this EnemyType type)
    {
        return type switch
        {
            EnemyType.Boss => Settings.BossRandoType.Value,
            EnemyType.Enemy => Settings.EnemyRandoType.Value,
            _ => Settings.MiscRandoType.Value
        };
    }

    public static EnemyType GetEnemyType(HealthManager hm)
    {
        var par = hm.transform.parent;
        if (par)
            if (par.name.Contains("Shellwood Hive"))
                return EnemyType.Misc;
        var deathEffects = hm.GetComponent<EnemyDeathEffects>();
        if (deathEffects is EnemyDeathEffectsNoEffect) return EnemyType.Misc;
        
        if (hm.name.Contains("Phantom") ||
            hm.name.Contains("Lace") ||
            hm.name.Contains("Silk Boss") ||
            hm.name.Contains("Bone Beast") ||
            hm.name.Contains("SG_head")) return EnemyType.Boss;
        foreach (var fsm in hm.GetComponents<PlayMakerFSM>())
        {
            foreach (var state in fsm.FsmStates)
            {
                if (state?.actions == null) continue;
                if (state.actions.Any(a => a is DisplayBossTitle)) return EnemyType.Boss;
            }
        }

        return EnemyType.Enemy;
    }
}