using UnityEngine;
using UnityEngine.UI;

public class UI_Options : MonoBehaviour
{
    private const string MasterVolumeKey = "master_volume";
    private const string FullscreenKey = "fullscreen_enabled";

    [Header("Runtime-bound UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private GameObject mainPanel;

    private bool listenersWired;

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
        float clampedValue = Mathf.Clamp01(value);
        AudioListener.volume = clampedValue;
        PlayerPrefs.SetFloat(MasterVolumeKey, clampedValue);
        PlayerPrefs.Save();
    }

    public void FullscreenValue(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void GoMainMenuBTN()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadMainMenu();
        }
    }

    public void CloseBTN()
    {
        gameObject.SetActive(false);
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }

    public void LoadUpSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.6f);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

        AudioListener.volume = savedVolume;
        Screen.fullScreen = fullscreen;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(savedVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }
    }

    public void Bind(Slider volumeSlider, Toggle fullscreen)
    {
        masterVolumeSlider = volumeSlider;
        fullscreenToggle = fullscreen;
        WireControls();
        LoadUpSettings();
    }

    private void ResolveHierarchyReferences()
    {
        if (masterVolumeSlider == null)
        {
            masterVolumeSlider = FindDeepChild(transform, "Master VolumeSlider")?.GetComponent<Slider>();
        }

        if (fullscreenToggle == null)
        {
            fullscreenToggle = FindDeepChild(transform, "FullscreenToggle")?.GetComponent<Toggle>();
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
        if (listenersWired)
        {
            return;
        }

        if (masterVolumeSlider != null && masterVolumeSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            masterVolumeSlider.onValueChanged.AddListener(MasterVolumeValue);
        }

        if (fullscreenToggle != null && fullscreenToggle.onValueChanged.GetPersistentEventCount() == 0)
        {
            fullscreenToggle.onValueChanged.AddListener(FullscreenValue);
        }

        listenersWired = true;
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
}
