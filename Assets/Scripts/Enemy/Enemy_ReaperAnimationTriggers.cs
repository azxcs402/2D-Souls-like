using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ReaperAnimationTriggers : MonoBehaviour
{
    private Enemy_Reaper enemyReaper;

    private void Awake()
    {
        enemyReaper = GetComponentInParent<Enemy_Reaper>();
    }

    public void TeleportTrigger()
    {
        if (enemyReaper != null)
        {
            enemyReaper.SetTeleportTrigger(true);
        }
    }
}
