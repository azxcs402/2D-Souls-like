using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private const string BgmMixerParam = "bgmMixer";
    private const string SfxMixerParam = "sfxMixer";
    private const float MinDecibels = -80f;

    [SerializeField] private AudioDatabaseSO audioDB;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [Space]
    private Transform player;

    private AudioClip lastMusicPlayed;
    private string currentBgmGroupName;
    private Coroutine currentBgmCo;
    private Coroutine currentBgmStopCo;
    private Coroutine volumeApplyCo;
    private float masterVolume = AudioVolumeDefaults.Master;
    private float bgmVolume = AudioVolumeDefaults.Bgm;
    private float sfxVolume = AudioVolumeDefaults.Sfx;
    [SerializeField] private bool bgmShouldPlay;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        EnsureAudioDatabase();
        ApplyMixerRouting();
        InitializeVolumeSettings();
    }

    private void OnValidate()
    {
        ApplyMixerRouting();
    }

    private void Update()
    {
        if (bgmSource == null || audioDB == null)
        {
            return;
        }

        if (bgmSource.isPlaying == false && bgmShouldPlay)
        {
            if (string.IsNullOrEmpty(currentBgmGroupName) == false)
            {
                NextBGM(currentBgmGroupName);
            }
        }

        if (bgmSource.isPlaying && bgmShouldPlay == false)
        {
            StopBGM();
        }
    }

    public void StartBGM(AudioKey musicGroup)
    {
        PlayBgmByName(AudioKeyMap.GetAudioName(musicGroup), false);
    }

    public void StartBGM(string musicGroup)
    {
        PlayBgmByName(musicGroup, false);
    }

    public void NextBGM(AudioKey musicGroup)
    {
        PlayBgmByName(AudioKeyMap.GetAudioName(musicGroup), true);
    }

    public void NextBGM(string musicGroup)
    {
        PlayBgmByName(musicGroup, true);
    }

    private void PlayBgmByName(string musicGroup, bool forceRestart)
    {
        bgmShouldPlay = true;

        if (bgmSource == null || !TryResolveAudioData(musicGroup, out string resolvedName, out AudioClipData data))
        {
            return;
        }

        if (!forceRestart && resolvedName == currentBgmGroupName && bgmSource.isPlaying)
        {
            return;
        }

        currentBgmGroupName = resolvedName;

        CancelBgmStop();

        if (currentBgmCo != null)
        {
            StopCoroutine(currentBgmCo);
        }

        currentBgmCo = StartCoroutine(SwitchMusicCo(data));
    }

    public void StopBGM()
    {
        bgmShouldPlay = false;

        if (bgmSource == null)
        {
            return;
        }

        if (currentBgmStopCo != null)
        {
            return;
        }

        if (currentBgmCo != null)
        {
            StopCoroutine(currentBgmCo);
            currentBgmCo = null;
        }

        currentBgmStopCo = StartCoroutine(StopBGMCo());
    }

    private IEnumerator StopBGMCo()
    {
        if (bgmSource == null)
        {
            yield break;
        }

        yield return FadeVolumeCo(bgmSource, 0f, 1f);
        bgmSource.Stop();
        currentBgmStopCo = null;
    }

    private IEnumerator SwitchMusicCo(AudioClipData data)
    {
        if (bgmSource == null)
        {
            yield break;
        }

        if (!data.TryGetRandomClip(out AudioClip nextMusic))
        {
            yield break;
        }

        if (data.clips != null && data.clips.Count > 1 && nextMusic == lastMusicPlayed)
        {
            const int maxAttempts = 8;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (!data.TryGetRandomClip(out nextMusic))
                {
                    yield break;
                }

                if (nextMusic != lastMusicPlayed)
                {
                    break;
                }
            }
        }

        if (bgmSource.isPlaying)
        {
            yield return FadeVolumeCo(bgmSource, 0f, 1f);
        }

        lastMusicPlayed = nextMusic;
        bgmSource.clip = nextMusic;
        bgmSource.volume = 0f;
        bgmSource.Play();

        StartCoroutine(FadeVolumeCo(bgmSource, data.maxVolume, 1f));
    }

    private IEnumerator FadeVolumeCo(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float time = 0f;
        float startVolume = source.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    public void PlaySFX(AudioKey soundName, AudioSource sfxSource, float minDistanceToHearSound = 5f)
    {
        PlayAudioByName(AudioKeyMap.GetAudioName(soundName), sfxSource, minDistanceToHearSound, true);
    }

    public void PlaySFX(string soundName, AudioSource sfxSource, float minDistanceToHearSound = 5f)
    {
        PlayAudioByName(soundName, sfxSource, minDistanceToHearSound, true);
    }

    public void PlayGlobalSFX(AudioKey soundName)
    {
        PlayAudioByName(AudioKeyMap.GetAudioName(soundName), sfxSource, 5f, false);
    }

    public void PlayGlobalSFX(string soundName)
    {
        PlayAudioByName(soundName, sfxSource, 5f, false);
    }

    private void PlayAudioByName(string soundName, AudioSource source, float minDistanceToHearSound, bool useDistanceFalloff)
    {
        if (source == null || !TryResolveAudioData(soundName, out _, out AudioClipData data))
        {
            return;
        }

        if (player == null)
        {
            player = FindPlayerTransform();
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return;
        }

        float volume = data.maxVolume;
        if (useDistanceFalloff)
        {
            float distance = player != null
                ? Vector2.Distance(source.transform.position, player.position)
                : 0f;
            float t = Mathf.Clamp01(1f - (distance / minDistanceToHearSound));
            volume = Mathf.Lerp(0f, data.maxVolume, t * t);
        }

        source.pitch = Random.Range(.95f, 1.1f);
        source.volume = volume;
        source.PlayOneShot(clip);
    }

    public void SetMasterVolume(float value)
    {
        SetVolumeSetting(ref masterVolume, AudioVolumeKeys.Master, value);
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public void SetBgmVolume(float value)
    {
        SetVolumeSetting(ref bgmVolume, AudioVolumeKeys.Bgm, value);
    }

    public float GetBgmVolume()
    {
        return bgmVolume;
    }

    public void SetSfxVolume(float value)
    {
        SetVolumeSetting(ref sfxVolume, AudioVolumeKeys.Sfx, value);
    }

    public float GetSfxVolume()
    {
        return sfxVolume;
    }

    public void SetVolumeSettings(float master, float bgm, float sfx)
    {
        masterVolume = Mathf.Clamp01(master);
        bgmVolume = Mathf.Clamp01(bgm);
        sfxVolume = Mathf.Clamp01(sfx);

        PersistVolumeSettings();
    }

    private void CancelBgmStop()
    {
        if (currentBgmStopCo != null)
        {
            StopCoroutine(currentBgmStopCo);
            currentBgmStopCo = null;
        }
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            bgmSource = CreateSource("BGM Source");
        }

        if (sfxSource == null)
        {
            sfxSource = CreateSource("SFX Source");
        }
    }

    private AudioSource CreateSource(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        AudioSource source = child.GetComponent<AudioSource>();
        if (source == null)
        {
            source = child.gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.volume = Mathf.Clamp01(source.volume);
        return source;
    }

    private void ApplyMixerRouting()
    {
        if (bgmSource != null)
        {
            bgmSource.outputAudioMixerGroup = ResolveMixerGroup(bgmMixerGroup, "Background music");
        }

        if (sfxSource != null)
        {
            sfxSource.outputAudioMixerGroup = ResolveMixerGroup(sfxMixerGroup, "Sound effects");
        }
    }

    private void ApplyAllVolumeSettings()
    {
        ApplyMixerVolumes(masterVolume, bgmVolume, sfxVolume);
    }

    private void InitializeVolumeSettings()
    {
        LoadSavedVolumeSettings();
        ApplyAllVolumeSettings();
    }

    private void LoadSavedVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(AudioVolumeKeys.Master, AudioVolumeDefaults.Master);
        bgmVolume = PlayerPrefs.GetFloat(AudioVolumeKeys.Bgm, AudioVolumeDefaults.Bgm);
        sfxVolume = PlayerPrefs.GetFloat(AudioVolumeKeys.Sfx, AudioVolumeDefaults.Sfx);
    }

    private void QueueVolumeApply()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (volumeApplyCo != null)
        {
            return;
        }

        volumeApplyCo = StartCoroutine(ApplyVolumeSettingsNextFrameCo());
    }

    private void SetVolumeSetting(ref float volumeField, string prefsKey, float value)
    {
        volumeField = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(prefsKey, volumeField);
        PlayerPrefs.Save();
        QueueVolumeApply();
    }

    private void PersistVolumeSettings()
    {
        PlayerPrefs.SetFloat(AudioVolumeKeys.Master, masterVolume);
        PlayerPrefs.SetFloat(AudioVolumeKeys.Bgm, bgmVolume);
        PlayerPrefs.SetFloat(AudioVolumeKeys.Sfx, sfxVolume);
        PlayerPrefs.Save();
        QueueVolumeApply();
    }

    private IEnumerator ApplyVolumeSettingsNextFrameCo()
    {
        yield return null;
        ApplyAllVolumeSettings();
        volumeApplyCo = null;
    }

    private void ApplyMixerVolumes(float masterVolume, float bgmVolume, float sfxVolume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(BgmMixerParam, LinearToDecibels(masterVolume * bgmVolume));
            audioMixer.SetFloat(SfxMixerParam, LinearToDecibels(masterVolume * sfxVolume));
        }
        else
        {
            AudioListener.volume = masterVolume;
        }
    }

    private static float LinearToDecibels(float normalizedVolume)
    {
        if (normalizedVolume <= 0f)
        {
            return MinDecibels;
        }

        return Mathf.Clamp(20f * Mathf.Log10(normalizedVolume), MinDecibels, 0f);
    }

    private AudioMixerGroup ResolveMixerGroup(AudioMixerGroup configuredGroup, string groupName)
    {
        if (configuredGroup != null)
        {
            return configuredGroup;
        }

        if (audioMixer != null)
        {
            AudioMixerGroup[] matchingGroups = audioMixer.FindMatchingGroups(groupName);
            if (matchingGroups != null && matchingGroups.Length > 0)
            {
                return matchingGroups[0];
            }
        }

        AudioMixerGroup[] loadedGroups = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
        for (int i = 0; i < loadedGroups.Length; i++)
        {
            AudioMixerGroup group = loadedGroups[i];
            if (group != null && group.name == groupName)
            {
                return group;
            }
        }

        return null;
    }

    private void EnsureAudioDatabase()
    {
        if (audioDB != null)
        {
            return;
        }

        audioDB = Resources.Load<AudioDatabaseSO>("Audio/AUDIO DATABASE");
    }

    private bool TryResolveAudioData(string audioNameOrKey, out string resolvedName, out AudioClipData data)
    {
        data = null;
        resolvedName = audioNameOrKey;
        if (audioDB == null)
        {
            return false;
        }

        resolvedName = AudioKeyMap.TryParse(audioNameOrKey, out AudioKey audioKey)
            ? AudioKeyMap.GetAudioName(audioKey)
            : audioNameOrKey;

        return audioDB.TryGet(resolvedName, out data);
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
