using UnityEngine;

public class Entity_SFX : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("SFX Names")]
    [SerializeField] private string attackHit = "player_attackHit";
    [SerializeField] private string attackMiss = "player_attackMiss";
    [Space]
    [SerializeField] private float soundDistance = 15f;
    [SerializeField] private bool showGizmo;


    private void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    public void PlayAttackHit()
    {
        if (AudioManager.instance != null)
        {
            if (AudioKeyMap.TryParse(attackHit, out AudioKey audioKey))
            {
                AudioManager.instance.PlaySFX(audioKey, audioSource, soundDistance);
                return;
            }

            AudioManager.instance.PlaySFX(attackHit, audioSource, soundDistance);
        }
    }

    public void PlayAttackMiss()
    {
        if (AudioManager.instance != null)
        {
            if (AudioKeyMap.TryParse(attackMiss, out AudioKey audioKey))
            {
                AudioManager.instance.PlaySFX(audioKey, audioSource, soundDistance);
                return;
            }

            AudioManager.instance.PlaySFX(attackMiss, audioSource, soundDistance);
        }
    }

    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, soundDistance);
        }
    }
}
