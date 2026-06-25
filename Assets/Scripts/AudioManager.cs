using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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
    private AudioClip lastMusicPlayed;
    private float currentBgmBaseVolume = 1f;
    private string currentBgmGroupName;
    private Coroutine currentBgmCo;
    private Coroutine currentBgmStopCo;
    private Coroutine volumeApplyCo;
    private AudioListener runtimeAudioListener;
    private readonly Dictionary<AudioSource, float> trackedSfxSourceBaseVolumes = new();
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
        ResolveAudioMixerReferences();
        EnsureSources();
        EnsureAudioDatabase();
        ApplyMixerRouting();
        InitializeVolumeSettings();
        EnsureAudioListener();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void OnValidate()
    {
        ResolveAudioMixerReferences();
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListener();
        ApplySceneBgm(scene.name);
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
        currentBgmBaseVolume = data.maxVolume;

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

        StartCoroutine(FadeVolumeCo(bgmSource, GetBgmTargetVolume(), 1f));
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

    public void PlaySFX(AudioKey soundName, AudioSource sfxSource, float maxHearDistance = 5f)
    {
        PlayAudioByName(AudioKeyMap.GetAudioName(soundName), sfxSource, maxHearDistance, true);
    }

    public void PlaySFX(string soundName, AudioSource sfxSource, float maxHearDistance = 5f)
    {
        PlayAudioByName(soundName, sfxSource, maxHearDistance, true);
    }

    public void PlayGlobalSFX(AudioKey soundName)
    {
        PlayAudioByName(AudioKeyMap.GetAudioName(soundName), sfxSource, 5f, false, 1f);
    }

    public void PlayGlobalSFX(string soundName)
    {
        PlayAudioByName(soundName, sfxSource, 5f, false, 1f);
    }

    public void PlayGlobalSFX(AudioKey soundName, float volumeMultiplier)
    {
        PlayAudioByName(AudioKeyMap.GetAudioName(soundName), sfxSource, 5f, false, volumeMultiplier);
    }

    public void PlayGlobalSFX(string soundName, float volumeMultiplier)
    {
        PlayAudioByName(soundName, sfxSource, 5f, false, volumeMultiplier);
    }

    public bool PlayGlobalSFXInstance(AudioKey soundName, float volumeMultiplier, out AudioSource source, out float baseVolume)
    {
        return PlayGlobalSFXInstance(AudioKeyMap.GetAudioName(soundName), volumeMultiplier, out source, out baseVolume);
    }

    public bool PlayGlobalSFXInstance(string soundName, float volumeMultiplier, out AudioSource source, out float baseVolume)
    {
        source = null;
        baseVolume = 0f;

        if (!TryResolveAudioData(soundName, out _, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        GameObject tempObject = new GameObject($"Temp SFX: {soundName}");
        source = tempObject.AddComponent<AudioSource>();
        ConfigurePlaybackSource(source, false, 0f);

        float pitch = Random.Range(.95f, 1.1f);
        baseVolume = data.maxVolume;
        source.pitch = pitch;
        ApplySfxSourceVolume(source, baseVolume * Mathf.Clamp01(volumeMultiplier));
        source.clip = clip;
        source.Play();

        float lifetime = (clip.length / Mathf.Max(.01f, pitch)) + 0.25f;
        Object.Destroy(tempObject, lifetime);
        return true;
    }

    public bool PlayLocalizedSFX(AudioKey soundName, AudioSource source, float maxHearDistance)
    {
        return PlayLocalizedSFX(AudioKeyMap.GetAudioName(soundName), source, maxHearDistance);
    }

    public bool PlayLocalizedSFX(string soundName, AudioSource source, float maxHearDistance)
    {
        if (source == null)
        {
            return false;
        }

        if (!TryResolveAudioData(soundName, out _, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        ConfigurePlaybackSource(source, true, maxHearDistance);
        source.pitch = Random.Range(.95f, 1.1f);
        ApplySfxSourceVolume(source, data.maxVolume);
        source.PlayOneShot(clip);
        return true;
    }

    public bool PlayLocalizedSFX(AudioKey soundName, Vector3 position, float maxHearDistance)
    {
        return PlayLocalizedSFX(AudioKeyMap.GetAudioName(soundName), position, maxHearDistance);
    }

    public bool PlayLocalizedSFX(string soundName, Vector3 position, float maxHearDistance)
    {
        if (!TryResolveAudioData(soundName, out _, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        float maxDistance = Mathf.Max(0.01f, maxHearDistance > 0f ? maxHearDistance : data.maxHearDistance);
        GameObject tempObject = new GameObject($"Temp SFX: {soundName}");
        tempObject.transform.position = position;
        AudioSource tempSource = tempObject.AddComponent<AudioSource>();
        ConfigurePlaybackSource(tempSource, true, maxDistance);

        float pitch = Random.Range(.95f, 1.1f);
        tempSource.pitch = pitch;
        ApplySfxSourceVolume(tempSource, data.maxVolume);
        tempSource.clip = clip;
        tempSource.Play();

        float lifetime = (clip.length / Mathf.Max(.01f, pitch)) + 0.25f;
        Object.Destroy(tempObject, lifetime);
        return true;
    }

    public bool PlayLoopingSFX(AudioKey soundName, AudioSource loopSource, float volumeMultiplier = 1f)
    {
        return PlayLoopingSFX(AudioKeyMap.GetAudioName(soundName), loopSource, volumeMultiplier);
    }

    public bool PlayLoopingSFX(string soundName, AudioSource loopSource, float volumeMultiplier = 1f)
    {
        if (loopSource == null || !TryResolveAudioData(soundName, out _, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        float targetVolume = Mathf.Clamp01(data.maxVolume * Mathf.Clamp01(volumeMultiplier));
        bool clipChanged = loopSource.clip != clip;

        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.pitch = 1f;
        ApplySfxSourceVolume(loopSource, targetVolume);
        loopSource.clip = clip;
        loopSource.outputAudioMixerGroup = ResolveMixerGroup(sfxMixerGroup, "Sound effects");

        if (clipChanged && loopSource.isPlaying)
        {
            loopSource.Stop();
        }

        if (!loopSource.isPlaying)
        {
            loopSource.Play();
        }

        return true;
    }

    public void StopLoopingSFX(AudioSource loopSource)
    {
        if (loopSource == null)
        {
            return;
        }

        if (loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    private bool PlayAudioByName(string soundName, AudioSource source, float maxHearDistance, bool useDistanceFalloff, float volumeMultiplier = 1f)
    {
        if (source == null || !TryResolveAudioData(soundName, out _, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        if (useDistanceFalloff)
        {
            float maxDistance = Mathf.Max(0.01f, maxHearDistance > 0f ? maxHearDistance : data.maxHearDistance);
            ConfigurePlaybackSource(source, true, maxDistance);
        }
        else
        {
            ConfigurePlaybackSource(source, false, 0f);
        }

        source.pitch = Random.Range(.95f, 1.1f);
        ApplySfxSourceVolume(source, data.maxVolume * Mathf.Clamp01(volumeMultiplier));
        source.PlayOneShot(clip);
        return true;
    }

    private void ConfigurePlaybackSource(AudioSource source, bool spatial, float maxHearDistance)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = false;
        source.outputAudioMixerGroup = ResolveMixerGroup(sfxMixerGroup, "Sound effects");

        if (spatial)
        {
            float maxDistance = Mathf.Max(0.01f, maxHearDistance);
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            source.minDistance = Mathf.Min(0.1f, maxDistance);
            source.maxDistance = maxDistance;
        }
        else
        {
            source.spatialBlend = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0f;
            source.minDistance = 0f;
            source.maxDistance = 0f;
        }
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

    private void ResolveAudioMixerReferences()
    {
        if (audioMixer == null)
        {
            AudioMixer[] loadedMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
            for (int i = 0; i < loadedMixers.Length; i++)
            {
                AudioMixer mixer = loadedMixers[i];
                if (mixer != null && mixer.name == "AudioMixer")
                {
                    audioMixer = mixer;
                    break;
                }
            }
        }

        if (audioMixer == null)
        {
            return;
        }

        if (bgmMixerGroup == null)
        {
            bgmMixerGroup = FindMixerGroup(audioMixer, "Background music");
        }

        if (sfxMixerGroup == null)
        {
            sfxMixerGroup = FindMixerGroup(audioMixer, "Sound effects");
        }
    }

    private static AudioMixerGroup FindMixerGroup(AudioMixer mixer, string groupName)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(groupName))
        {
            return null;
        }

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        if (groups != null && groups.Length > 0)
        {
            return groups[0];
        }

        return null;
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
        ApplyAllVolumeSettings();
    }

    private void PersistVolumeSettings()
    {
        PlayerPrefs.SetFloat(AudioVolumeKeys.Master, masterVolume);
        PlayerPrefs.SetFloat(AudioVolumeKeys.Bgm, bgmVolume);
        PlayerPrefs.SetFloat(AudioVolumeKeys.Sfx, sfxVolume);
        PlayerPrefs.Save();
        ApplyAllVolumeSettings();
    }

    private IEnumerator ApplyVolumeSettingsNextFrameCo()
    {
        yield return null;
        ApplyAllVolumeSettings();
        volumeApplyCo = null;
    }

    private void ApplyMixerVolumes(float masterVolume, float bgmVolume, float sfxVolume)
    {
        AudioListener.volume = masterVolume;

        if (audioMixer != null)
        {
            audioMixer.SetFloat(BgmMixerParam, LinearToDecibels(bgmVolume));
            audioMixer.SetFloat(SfxMixerParam, LinearToDecibels(sfxVolume));
        }
        else
        {
            ApplyFallbackSourceVolumes();
        }
    }

    private void ApplyFallbackSourceVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = currentBgmBaseVolume * bgmVolume;
        }

        if (trackedSfxSourceBaseVolumes.Count == 0)
        {
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }

            return;
        }

        List<AudioSource> staleSources = null;
        foreach (KeyValuePair<AudioSource, float> entry in trackedSfxSourceBaseVolumes)
        {
            AudioSource source = entry.Key;
            if (source == null)
            {
                staleSources ??= new List<AudioSource>();
                staleSources.Add(source);
                continue;
            }

            source.volume = entry.Value * sfxVolume;
        }

        if (staleSources == null)
        {
            return;
        }

        for (int i = 0; i < staleSources.Count; i++)
        {
            trackedSfxSourceBaseVolumes.Remove(staleSources[i]);
        }
    }

    private void ApplySfxSourceVolume(AudioSource source, float baseVolume)
    {
        if (source == null)
        {
            return;
        }

        float resolvedBaseVolume = Mathf.Max(0f, baseVolume);

        if (audioMixer != null)
        {
            source.volume = resolvedBaseVolume;
            return;
        }

        trackedSfxSourceBaseVolumes[source] = resolvedBaseVolume;
        source.volume = resolvedBaseVolume * sfxVolume;
    }

    private float GetBgmTargetVolume()
    {
        float resolvedBaseVolume = Mathf.Max(0f, currentBgmBaseVolume);
        return audioMixer != null ? resolvedBaseVolume : resolvedBaseVolume * bgmVolume;
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

    private void EnsureAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool hasOtherEnabledListener = false;

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener == null)
            {
                continue;
            }

            if (listener.gameObject == gameObject)
            {
                runtimeAudioListener = listener;
                continue;
            }

            if (listener.enabled && listener.gameObject.activeInHierarchy)
            {
                hasOtherEnabledListener = true;
                break;
            }
        }

        if (hasOtherEnabledListener)
        {
            if (runtimeAudioListener != null && runtimeAudioListener.gameObject == gameObject)
            {
                Destroy(runtimeAudioListener);
                runtimeAudioListener = null;
            }

            return;
        }

        if (runtimeAudioListener == null)
        {
            runtimeAudioListener = gameObject.GetComponent<AudioListener>();
            if (runtimeAudioListener == null)
            {
                runtimeAudioListener = gameObject.AddComponent<AudioListener>();
            }
        }
    }

    private void ApplySceneBgm(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (sceneName == "MainMenu")
        {
            StartBGM(AudioKey.PlaylistMainMenu);
            return;
        }

        StartBGM(AudioKey.PlaylistLevels);
    }

}
