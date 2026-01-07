using BepInEx.Configuration;
using UnityEngine;

namespace EnemyRando;

public static class Settings
{
    public static ConfigEntry<ConsistencyType> Consistency;
    public static ConfigEntry<int> Seed;
    
    public static ConfigEntry<int> Multiplier;
    
    public static ConfigEntry<RandoType> EnemyRandoType;
    public static ConfigEntry<RandoType> BossRandoType;
    public static ConfigEntry<RandoType> MiscRandoType;
    
    public static ConfigEntry<float> BlackThreadChance;
    public static ConfigEntry<float> BlackThreadChanceAct3;
    public static ConfigEntry<float> PlasmifiedChance;
    public static ConfigEntry<float> PlasmifiedChanceAct3;
    
    public static ConfigEntry<bool> MaintainHp;
    
    public static void Init(ConfigFile config)
    {
        Seed = config.Bind(
            "Options",
            "Seed",
            Random.RandomRangeInt(0, 99999),
            "The seed for the randomisation.\n" +
            "Only has an effect if Consistency is set to EnemyType, Individual or Room."
        );
        Consistency = config.Bind(
            "Options",
            "ConsistencyMode",
            ConsistencyType.None,
            "Whether enemies should be consistently randomised to the same thing.\n" +
            "None means they are random each time,\n" +
            "EnemyType will randomise enemies of the same type to the same thing,\n" +
            "Individual will randomise each individual enemy to the same thing each time.\n" +
            "Room will randomise each enemy in a room to the same thing."
        );
        
        Multiplier = config.Bind(
            "Options",
            "Multiplier",
            1,
            "How many copies of randomised enemies should be spawned."
        );
        
        MaintainHp = config.Bind(
            "Options",
            "MaintainHp",
            false,
            "Controls whether replacement enemies should have the same health as the ones they replace."
        );

        const string randoDesc =
            "'Disabled' - Will not be randomised.\n" +
            "'Any' - Can randomise into enemies or bosses.\n" +
            "'Enemy' - Can only randomise into regular enemies.\n" +
            "'Boss' - Can only randomise into bosses.";
        
        EnemyRandoType = config.Bind(
            "Targets",
            "EnemyRandoType",
            RandoType.Any,
            "What enemies should be able to turn into.\n" + randoDesc
        );
        
        BossRandoType = config.Bind(
            "Targets",
            "BossRandoType",
            RandoType.Any,
            "What bosses should be able to turn into.\n" + randoDesc
        );
        
        MiscRandoType = config.Bind(
            "Targets",
            "MiscRandoType",
            RandoType.Disabled,
            "What non living objects with health managers (e.g. moss cocoons) should be able to turn into.\n"
            + randoDesc
        );
        
        BlackThreadChance = config.Bind(
            "States",
            "BlackThreadChance",
            0f,
            "The chance of randomised enemies becoming black threaded outside of Act 3."
        );
        
        BlackThreadChanceAct3 = config.Bind(
            "States",
            "BlackThreadChanceAct3",
            0.5f,
            "The chance of randomised enemies becoming black threaded in Act 3."
        );
        
        PlasmifiedChance = config.Bind(
            "States",
            "PlasmifiedChance",
            0f,
            "The chance of randomised enemies becoming plasmified outside of Act 3."
        );
        
        PlasmifiedChanceAct3 = config.Bind(
            "States",
            "PlasmifiedChanceAct3",
            0f,
            "The chance of randomised enemies becoming plasmified in Act 3."
        );
    }
    
    public enum ConsistencyType {
        None,
        EnemyType,
        Individual,
        Room
    }

    public enum RandoType
    {
        Disabled,
        Any,
        Enemy,
        Boss
    }
}