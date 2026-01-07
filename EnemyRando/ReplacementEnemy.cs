using UnityEngine;

namespace EnemyRando;

public class BlockAudio : MonoBehaviour;

public class ReplacementEnemy : BlockAudio
{
    public HealthManager? target;
    public Settings.RandoType randoType;
}