using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UI_Options : MonoBehaviour
{
    private enum VolumeChannel
    {
        Master,
        Bgm,
        Sfx
    }

    private static readonly Dictionary<VolumeChannel, string> VolumePrefsKeys = new()
    {
        [VolumeChannel.Master] = AudioVolumeKeys.Master,
        [VolumeChannel.Bgm] = AudioVolumeKeys.Bgm,
        [VolumeChannel.Sfx] = AudioVolumeKeys.Sfx
    };

    [Header("Runtime-bound UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private GameObject mainPanel;

    private bool masterListenerWired;
    private bool bgmListenerWired;
    private bool sfxListenerWired;

    private void Awake()
    {
        ResolveHierarchyReferences();
        WireControls();
    }

    public void Configure(GameObject mainPanelRoot)
    {
        mainPanel = mainPanelRoot;
        ResolveHierarchyReferences();
        WireControls();
    }

    private void Start()
    {
        ResolveHierarchyReferences();
        WireControls();
        LoadUpSettings();
    }

    public void MasterVolumeValue(float value)
    {
        ApplySingleVolumeSetting(VolumeChannel.Master, value, clampedValue => AudioListener.volume = clampedValue);
    }

    public void BgmVolumeValue(float value)
    {
        ApplySingleVolumeSetting(VolumeChannel.Bgm, value, null);
    }

    public void SfxVolumeValue(float value)
    {
        ApplySingleVolumeSetting(VolumeChannel.Sfx, value, null);
    }

    public void ResetVolumeDefaults()
    {
        UIAudio.PlayButtonClick();
        ApplyVolumeState(new VolumeState(AudioVolumeDefaults.Master, AudioVolumeDefaults.Bgm, AudioVolumeDefaults.Sfx));
    }

    public void GoMainMenuBTN()
    {
        UIAudio.PlayButtonClick();

        if (GameManager.instance != null)
        {
            GameManager.instance.LoadMainMenu();
        }
    }

    public void CloseBTN()
    {
        UIAudio.PlayButtonClick();

        gameObject.SetActive(false);
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }

    public void LoadUpSettings()
    {
        VolumeState savedState = LoadVolumeState();

        ApplyVolumeState(savedState);
    }

    public void Bind(Slider volumeSlider, Slider bgmSlider, Slider sfxSlider)
    {
        masterVolumeSlider = volumeSlider;
        bgmVolumeSlider = bgmSlider;
        sfxVolumeSlider = sfxSlider;
        WireControls();
        LoadUpSettings();
    }

    private void ResolveHierarchyReferences()
    {
        if (masterVolumeSlider == null)
        {
            masterVolumeSlider = FindDeepChild(transform, "Master VolumeSlider")?.GetComponent<Slider>();
        }

        if (bgmVolumeSlider == null)
        {
            bgmVolumeSlider = FindDeepChild(transform, "BGM VolumeSlider")?.GetComponent<Slider>();
        }

        if (sfxVolumeSlider == null)
        {
            sfxVolumeSlider = FindDeepChild(transform, "SFX VolumeSlider")?.GetComponent<Slider>();
        }

        if (mainPanel == null && transform.parent != null)
        {
            Transform mainPanelTransform = transform.parent.Find("MainPanel");
            if (mainPanelTransform != null)
            {
                mainPanel = mainPanelTransform.gameObject;
            }
        }
    }

    private void WireControls()
    {
        if (masterVolumeSlider != null && !masterListenerWired && masterVolumeSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            masterVolumeSlider.onValueChanged.AddListener(MasterVolumeValue);
            masterListenerWired = true;
        }

        if (bgmVolumeSlider != null && !bgmListenerWired && bgmVolumeSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            bgmVolumeSlider.onValueChanged.AddListener(BgmVolumeValue);
            bgmListenerWired = true;
        }

        if (sfxVolumeSlider != null && !sfxListenerWired && sfxVolumeSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SfxVolumeValue);
            sfxListenerWired = true;
        }
    }

    private void ApplyVolumeState(VolumeState state)
    {
        float clampedMaster = Mathf.Clamp01(state.master);
        float clampedBgm = Mathf.Clamp01(state.bgm);
        float clampedSfx = Mathf.Clamp01(state.sfx);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetVolumeSettings(clampedMaster, clampedBgm, clampedSfx);
        }
        else
        {
            AudioListener.volume = clampedMaster;
            SaveVolumePrefs(clampedMaster, clampedBgm, clampedSfx);
        }

        ApplyVolumeUIState(new VolumeState(clampedMaster, clampedBgm, clampedSfx));
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void ApplySingleVolumeSetting(VolumeChannel channel, float value, System.Action<float> applyFallback)
    {
        float clampedValue = Mathf.Clamp01(value);
        string prefsKey = VolumePrefsKeys[channel];

        if (AudioManager.instance != null)
        {
            switch (channel)
            {
                case VolumeChannel.Master:
                    AudioManager.instance.SetMasterVolume(clampedValue);
                    break;
                case VolumeChannel.Bgm:
                    AudioManager.instance.SetBgmVolume(clampedValue);
                    break;
                case VolumeChannel.Sfx:
                    AudioManager.instance.SetSfxVolume(clampedValue);
                    break;
            }

            return;
        }

        applyFallback?.Invoke(clampedValue);
        PlayerPrefs.SetFloat(prefsKey, clampedValue);
        PlayerPrefs.Save();
    }

    private static void SaveVolumePrefs(float master, float bgm, float sfx)
    {
        PlayerPrefs.SetFloat(VolumePrefsKeys[VolumeChannel.Master], master);
        PlayerPrefs.SetFloat(VolumePrefsKeys[VolumeChannel.Bgm], bgm);
        PlayerPrefs.SetFloat(VolumePrefsKeys[VolumeChannel.Sfx], sfx);
        PlayerPrefs.Save();
    }

    private VolumeState LoadVolumeState()
    {
        return new VolumeState(
            PlayerPrefs.GetFloat(VolumePrefsKeys[VolumeChannel.Master], AudioVolumeDefaults.Master),
            PlayerPrefs.GetFloat(VolumePrefsKeys[VolumeChannel.Bgm], AudioVolumeDefaults.Bgm),
            PlayerPrefs.GetFloat(VolumePrefsKeys[VolumeChannel.Sfx], AudioVolumeDefaults.Sfx));
    }

    private void ApplyVolumeUIState(VolumeState state)
    {
        masterVolumeSlider?.SetValueWithoutNotify(state.master);
        bgmVolumeSlider?.SetValueWithoutNotify(state.bgm);
        sfxVolumeSlider?.SetValueWithoutNotify(state.sfx);
    }

    private readonly struct VolumeState
    {
        public readonly float master;
        public readonly float bgm;
        public readonly float sfx;

        public VolumeState(float master, float bgm, float sfx)
        {
            this.master = master;
            this.bgm = bgm;
            this.sfx = sfx;
        }
    }
}
