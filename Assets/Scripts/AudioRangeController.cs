using UnityEngine;

public class AudioRangeController : MonoBehaviour
{
    private AudioSource source;
    private Transform player;

    [SerializeField] private float minDistanceToHearSound = 12;
    [SerializeField] private bool showGizmo;
    private float maxVolume;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        player = FindPlayerTransform();

        if (source != null)
        {
            maxVolume = source.volume;
        }
    }

    private void Update()
    {
        if (player == null)
            player = FindPlayerTransform();

        if (player == null || source == null)
            return;

        float distance = Vector2.Distance(player.position, transform.position);
        float t = Mathf.Clamp01(1 - (distance / minDistanceToHearSound));

        float targetVolume = Mathf.Lerp(0, maxVolume, t * t);
        source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 3);
    }

    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, minDistanceToHearSound);
        }
    }

    private static Transform FindPlayerTransform()
    {
#if UNITY_2023_1_OR_NEWER
        Player foundPlayer = FindFirstObjectByType<Player>();
#else
        Player foundPlayer = FindObjectOfType<Player>();
#endif
        return foundPlayer != null ? foundPlayer.transform : null;
    }
}
