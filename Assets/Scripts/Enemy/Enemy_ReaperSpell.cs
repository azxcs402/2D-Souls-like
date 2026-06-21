using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ReaperSpell : MonoBehaviour
{
    private const string WindLoopAudioSourceName = "WindLoopAudioSource";
    private static AudioDatabaseSO cachedAudioDatabase;

    [SerializeField] private LayerMask whatIsTarget;
    [SerializeField] private Collider2D col;
    [SerializeField, Min(0f)] private float damageActivationDelay = 0.45f;
    [SerializeField, Min(.01f)] private float lifeTime = 2f;
    [SerializeField, Min(1)] private int baseDamage = 1;
    [SerializeField] private AudioKey windLoopSfxKey = AudioKey.SpellWindLoop;
    [SerializeField] private AudioClip windLoopClip;
    [SerializeField, Range(0f, 1f)] private float windLoopVolume = 0.85f;
    [SerializeField, Min(0.01f)] private float windLoopMinDistance = 0.5f;
    [SerializeField, Min(0.01f)] private float windLoopMaxDistance = 8f;

    private Entity_Combat combat;
    private DamageScaleData damageScaleData;
    private Coroutine activationCoroutine;
    private AudioSource windLoopAudioSource;
    private bool windLoopStarted;

    private void Awake()
    {
        EnsureTargetMaskAssigned();

        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (col != null)
        {
            col.isTrigger = true;
        }

        EnsureWindLoopAudioSource();
        TryStartWindLoopAudio();
    }

    private void Reset()
    {
        EnsureTargetMaskAssigned();
    }

    private void OnValidate()
    {
        EnsureTargetMaskAssigned();
        windLoopVolume = Mathf.Clamp01(windLoopVolume);
        windLoopMinDistance = Mathf.Max(0.01f, windLoopMinDistance);
        windLoopMaxDistance = Mathf.Max(windLoopMinDistance, windLoopMaxDistance);
        EnsureWindLoopAudioSource();
    }

    private void OnDisable()
    {
        StopWindLoopAudio();
        windLoopStarted = false;
    }

    private void OnDestroy()
    {
        StopWindLoopAudio();
        windLoopStarted = false;
    }

    private void EnsureTargetMaskAssigned()
    {
        if (whatIsTarget == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                whatIsTarget = 1 << playerLayer;
            }
        }
    }

    public void SetupSpell(Entity_Combat combat, DamageScaleData damageScaleData)
    {
        this.combat = combat;
        this.damageScaleData = damageScaleData;

        DisableCollider();
        TryStartWindLoopAudio();

        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        activationCoroutine = StartCoroutine(EnableColliderAfterDelay());
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (windLoopAudioSource != null && !windLoopAudioSource.isPlaying)
        {
            windLoopStarted = false;
        }

        if (!windLoopStarted)
        {
            TryStartWindLoopAudio();
        }
    }

    private void TryStartWindLoopAudio()
    {
        if (windLoopStarted)
        {
            return;
        }

        EnsureWindLoopAudioSource();
        if (windLoopAudioSource == null)
        {
            return;
        }

        if (TryPlayConfiguredWindLoop())
        {
            windLoopStarted = true;
            return;
        }

        if (windLoopClip != null && TryPlayWindLoopClip(windLoopClip))
        {
            windLoopStarted = true;
            return;
        }

        if (AudioManager.instance != null && AudioManager.instance.PlayLoopingSFX(windLoopSfxKey, windLoopAudioSource, windLoopVolume))
        {
            windLoopStarted = true;
            return;
        }

        if (!TryPlayWindLoopFromDatabase())
        {
            return;
        }

        windLoopStarted = true;
    }

    private bool TryPlayConfiguredWindLoop()
    {
        if (windLoopAudioSource == null || windLoopAudioSource.clip == null)
        {
            return false;
        }

        windLoopAudioSource.playOnAwake = true;
        windLoopAudioSource.enabled = true;
        windLoopAudioSource.loop = true;
        windLoopAudioSource.spatialBlend = 1f;
        windLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        windLoopAudioSource.dopplerLevel = 0f;
        windLoopAudioSource.minDistance = windLoopMinDistance;
        windLoopAudioSource.maxDistance = windLoopMaxDistance;
        windLoopAudioSource.volume = Mathf.Clamp01(windLoopVolume);

        if (!windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Play();
        }

        return windLoopAudioSource.isPlaying;
    }

    public void EnableCollider()
    {
        if (col != null)
        {
            col.enabled = true;
        }
    }

    public void DisableCollider()
    {
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void StopWindLoopAudio()
    {
        if (windLoopAudioSource == null)
        {
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopLoopingSFX(windLoopAudioSource);
        }
        else if (windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Stop();
        }
    }

    private void EnsureWindLoopAudioSource()
    {
        if (windLoopAudioSource == null)
        {
            windLoopAudioSource = GetComponent<AudioSource>();
            if (windLoopAudioSource == null)
            {
                windLoopAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (windLoopAudioSource == null)
        {
            return;
        }

        windLoopAudioSource.playOnAwake = true;
        windLoopAudioSource.enabled = true;
        windLoopAudioSource.loop = true;
        windLoopAudioSource.spatialBlend = 1f;
        windLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        windLoopAudioSource.dopplerLevel = 0f;
        windLoopAudioSource.minDistance = windLoopMinDistance;
        windLoopAudioSource.maxDistance = windLoopMaxDistance;
    }

    private bool TryPlayWindLoopClip(AudioClip clip)
    {
        if (clip == null || windLoopAudioSource == null)
        {
            return false;
        }

        bool clipChanged = windLoopAudioSource.clip != clip;
        float targetVolume = Mathf.Clamp01(windLoopVolume);

        windLoopAudioSource.playOnAwake = true;
        windLoopAudioSource.enabled = true;
        windLoopAudioSource.loop = true;
        windLoopAudioSource.pitch = 1f;
        windLoopAudioSource.volume = targetVolume;
        windLoopAudioSource.clip = clip;
        windLoopAudioSource.spatialBlend = 1f;
        windLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        windLoopAudioSource.dopplerLevel = 0f;
        windLoopAudioSource.minDistance = windLoopMinDistance;
        windLoopAudioSource.maxDistance = windLoopMaxDistance;

        if (clipChanged && windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Stop();
        }

        if (!windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Play();
        }

        return windLoopAudioSource.isPlaying;
    }

    private bool TryPlayWindLoopFromDatabase()
    {
        AudioDatabaseSO database = GetAudioDatabase();
        if (database == null || !database.TryGet(windLoopSfxKey, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        return TryPlayWindLoopClip(clip);
    }

    private static AudioDatabaseSO GetAudioDatabase()
    {
        if (cachedAudioDatabase == null)
        {
            cachedAudioDatabase = Resources.Load<AudioDatabaseSO>("Audio/AUDIO DATABASE");
        }

        return cachedAudioDatabase;
    }

    private IEnumerator EnableColliderAfterDelay()
    {
        if (damageActivationDelay > 0f)
        {
            yield return new WaitForSeconds(damageActivationDelay);
        }

        EnableCollider();
        activationCoroutine = null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
        {
            return;
        }

        if (((1 << collision.gameObject.layer) & whatIsTarget.value) == 0)
        {
            return;
        }

        Entity_Health targetHealth = collision.GetComponentInParent<Entity_Health>();
        if (targetHealth == null)
        {
            return;
        }

        int damage = combat != null ? Mathf.Max(1, combat.Damage) : Mathf.Max(1, baseDamage);
        if (damageScaleData != null)
        {
            int scaledBaseDamage = combat != null ? combat.Damage : baseDamage;
            damage = Mathf.Max(1, Mathf.RoundToInt(scaledBaseDamage * Mathf.Max(.01f, damageScaleData.phyiscal)));
        }

        targetHealth.TakeDamage(damage, combat, Vector2.zero);
        DisableCollider();
    }
}
