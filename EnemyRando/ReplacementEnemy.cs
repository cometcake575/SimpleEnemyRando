using Architect.Utils;
using UnityEngine;

namespace EnemyRando;

public class BlockAudio : MonoBehaviour;

public class ReplacementEnemy : BlockAudio
{
    public HealthManager? target;
    public Settings.RandoType randoType;

    private void Update()
    {
        if (transform.position.y < -50)
        {
            gameObject.BroadcastEvent("OnDeath");
            Destroy(gameObject);
        }
    }
}