using UnityEngine;
using UnityEngine.UI;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private bool resumeOnPlay = false;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button continueButton;

    private bool listenersWired;

    private void Awake()
    {
        ResolveHierarchyReferences();
        WireButtons();
    }

    public void Configure(GameObject mainPanelRoot, GameObject optionsPanelRoot)
    {
        mainPanel = mainPanelRoot;
        optionsPanel = optionsPanelRoot;
        ResolveHierarchyReferences();
        WireButtons();
    }

    private void Start()
    {
        ResolveHierarchyReferences();
        WireButtons();

        if (GameManager.instance != null && GameManager.instance.IsMenuScene)
        {
            GameManager.instance.ResumeMenu();
        }

        if (continueButton != null)
        {
            bool hasSave = SaveManager.instance != null && SaveManager.instance.HasSaveData;
            continueButton.interactable = hasSave;
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    public void PlayBTN()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        if (resumeOnPlay)
        {
            GameManager.instance.ResumeGameplay();
            return;
        }

        GameManager.instance.PlayGame();
    }

    public void NewGameBTN()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        GameManager.instance.StartNewGame();
    }

    public void ContinueBTN()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        GameManager.instance.ContinuePlay();
    }

    public void OptionsBTN()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void QuitGameBTN()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.QuitGame();
        }
        else
        {
            Application.Quit();
        }
    }

    private void ResolveHierarchyReferences()
    {
        if (mainPanel == null)
        {
            Transform mainPanelTransform = transform.Find("MainPanel");
            if (mainPanelTransform != null)
            {
                mainPanel = mainPanelTransform.gameObject;
            }
        }

        if (optionsPanel == null)
        {
            Transform optionsPanelTransform = transform.Find("OptionsPanel");
            if (optionsPanelTransform != null)
            {
                optionsPanel = optionsPanelTransform.gameObject;
            }
        }

        if (continueButton == null && mainPanel != null)
        {
            continueButton = FindButton(mainPanel.transform, "ContinueButton");
        }
    }

    private void WireButtons()
    {
        if (listenersWired)
        {
            return;
        }

        Button playButton = FindButton(mainPanel != null ? mainPanel.transform : null, "PlayButton");
        Button continueButtonLocal = continueButton;
        Button newGameButton = FindButton(mainPanel != null ? mainPanel.transform : null, "NewGameButton");
        Button optionsButton = FindButton(mainPanel != null ? mainPanel.transform : null, "OptionsButton");
        Button quitButton = FindButton(mainPanel != null ? mainPanel.transform : null, "QuitButton");
        Button backButton = FindButton(optionsPanel != null ? optionsPanel.transform : null, "BackButton");

        if (playButton != null && playButton.onClick.GetPersistentEventCount() == 0)
        {
            playButton.onClick.AddListener(PlayBTN);
        }

        if (continueButtonLocal != null && continueButtonLocal.onClick.GetPersistentEventCount() == 0)
        {
            continueButtonLocal.onClick.AddListener(ContinueBTN);
        }

        if (newGameButton != null && newGameButton.onClick.GetPersistentEventCount() == 0)
        {
            newGameButton.onClick.AddListener(NewGameBTN);
        }

        if (optionsButton != null && optionsButton.onClick.GetPersistentEventCount() == 0)
        {
            optionsButton.onClick.AddListener(OptionsBTN);
        }

        if (quitButton != null && quitButton.onClick.GetPersistentEventCount() == 0)
        {
            quitButton.onClick.AddListener(QuitGameBTN);
        }

        if (backButton != null && backButton.onClick.GetPersistentEventCount() == 0)
        {
            backButton.onClick.AddListener(() =>
            {
                if (optionsPanel != null)
                {
                    optionsPanel.SetActive(false);
                }

                if (mainPanel != null)
                {
                    mainPanel.SetActive(true);
                }
            });
        }

        listenersWired = true;
    }

    private static Button FindButton(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
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
