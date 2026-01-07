using System.Collections.Generic;
using UnityEngine;

namespace EnemyRando;

public class ReplacedEnemy : MonoBehaviour
{
    public List<ReplacementEnemy> replacements = [];
    
    private void OnEnable()
    {
        gameObject.SetActive(false);
    }
}