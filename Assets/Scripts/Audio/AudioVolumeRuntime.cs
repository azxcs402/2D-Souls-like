using UnityEngine;
using UnityEngine.Audio;

public static class AudioVolumeRuntime
{
    private const string MixerName = "AudioMixer";
    private const string BgmMixerParam = "bgmMixer";
    private const string SfxMixerParam = "sfxMixer";
    private const float MinDecibels = -80f;

    public static float GetMasterVolume()
    {
        if (AudioManager.instance != null)
        {
            return AudioManager.instance.GetMasterVolume();
        }

        return PlayerPrefs.GetFloat(AudioVolumeKeys.Master, AudioVolumeDefaults.Master);
    }

    public static float GetBgmVolume()
    {
        if (AudioManager.instance != null)
        {
            return AudioManager.instance.GetBgmVolume();
        }

        return PlayerPrefs.GetFloat(AudioVolumeKeys.Bgm, AudioVolumeDefaults.Bgm);
    }

    public static float GetSfxVolume()
    {
        if (AudioManager.instance != null)
        {
            return AudioManager.instance.GetSfxVolume();
        }

        return PlayerPrefs.GetFloat(AudioVolumeKeys.Sfx, AudioVolumeDefaults.Sfx);
    }

    public static void SetMasterVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(clampedValue);
            return;
        }

        AudioListener.volume = clampedValue;
        SaveVolume(AudioVolumeKeys.Master, clampedValue);
    }

    public static void SetBgmVolume(float value)
    {
        SetChannelVolume(AudioVolumeKeys.Bgm, value, BgmMixerParam);
    }

    public static void SetSfxVolume(float value)
    {
        SetChannelVolume(AudioVolumeKeys.Sfx, value, SfxMixerParam);
    }

    public static void SetVolumeSettings(float master, float bgm, float sfx)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetVolumeSettings(master, bgm, sfx);
            return;
        }

        float clampedMaster = Mathf.Clamp01(master);
        float clampedBgm = Mathf.Clamp01(bgm);
        float clampedSfx = Mathf.Clamp01(sfx);

        AudioListener.volume = clampedMaster;
        ApplyMixerVolumes(clampedBgm, clampedSfx);

        SaveVolume(AudioVolumeKeys.Master, clampedMaster);
        SaveVolume(AudioVolumeKeys.Bgm, clampedBgm);
        SaveVolume(AudioVolumeKeys.Sfx, clampedSfx);
    }

    private static void SetChannelVolume(string prefsKey, float value, string mixerParam)
    {
        float clampedValue = Mathf.Clamp01(value);

        if (AudioManager.instance != null)
        {
            switch (prefsKey)
            {
                case AudioVolumeKeys.Bgm:
                    AudioManager.instance.SetBgmVolume(clampedValue);
                    return;
                case AudioVolumeKeys.Sfx:
                    AudioManager.instance.SetSfxVolume(clampedValue);
                    return;
            }
        }

        ApplyMixerVolume(mixerParam, clampedValue);
        SaveVolume(prefsKey, clampedValue);
    }

    private static void ApplyMixerVolumes(float bgm, float sfx)
    {
        ApplyMixerVolume(BgmMixerParam, bgm);
        ApplyMixerVolume(SfxMixerParam, sfx);
    }

    private static void ApplyMixerVolume(string mixerParam, float normalizedVolume)
    {
        AudioMixer mixer = ResolveAudioMixer();
        if (mixer == null)
        {
            return;
        }

        mixer.SetFloat(mixerParam, LinearToDecibels(normalizedVolume));
    }

    private static AudioMixer ResolveAudioMixer()
    {
        AudioMixer[] loadedMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        for (int i = 0; i < loadedMixers.Length; i++)
        {
            AudioMixer mixer = loadedMixers[i];
            if (mixer != null && mixer.name == MixerName)
            {
                return mixer;
            }
        }

        return null;
    }

    private static void SaveVolume(string prefsKey, float value)
    {
        PlayerPrefs.SetFloat(prefsKey, value);
        PlayerPrefs.Save();
    }

    private static float LinearToDecibels(float normalizedVolume)
    {
        if (normalizedVolume <= 0f)
        {
            return MinDecibels;
        }

        return Mathf.Clamp(20f * Mathf.Log10(normalizedVolume), MinDecibels, 0f);
    }
}
