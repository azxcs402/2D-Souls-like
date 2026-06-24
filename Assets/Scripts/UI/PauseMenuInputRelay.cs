using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuInputRelay : MonoBehaviour
{
    [SerializeField] private UI_PauseMenu pauseMenu;

    public void Initialize(UI_PauseMenu targetPauseMenu)
    {
        pauseMenu = targetPauseMenu;
    }

    private void Awake()
    {
        ResolvePauseMenu();
    }

    private void Start()
    {
        ResolvePauseMenu();
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.IsMenuScene)
        {
            return;
        }

        if (pauseMenu == null)
        {
            ResolvePauseMenu();
        }

        if (pauseMenu == null)
        {
            return;
        }

        bool escapePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || Input.GetKeyDown(KeyCode.Escape);
        if (!escapePressed)
        {
            return;
        }

        pauseMenu.HandleEscapeRequest();
    }

    private void ResolvePauseMenu()
    {
        if (pauseMenu != null)
        {
            return;
        }

        pauseMenu = Object.FindFirstObjectByType<UI_PauseMenu>(FindObjectsInactive.Include);
    }
}
