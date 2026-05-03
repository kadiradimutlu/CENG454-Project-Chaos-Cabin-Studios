using UnityEngine;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("References")]
    [SerializeField] private MainMenuManager mainMenuManager;
    [SerializeField] private GameObject gameWorld;

    [Header("Pause Panels")]
    [SerializeField] private GameObject pauseRootPanel;
    [SerializeField] private GameObject pauseMainPanel;
    [SerializeField] private GameObject pauseSettingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button backToMainMenuButton;
    [SerializeField] private Button quitButton;

    private bool _isLeavingOrQuitting;
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    private void Awake()
    {
        if (mainMenuManager == null)
            mainMenuManager = FindObjectOfType<MainMenuManager>();

        ClosePauseInstant();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettingsPanel);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(BackFromSettingsPanel);

        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(BackToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (_isLeavingOrQuitting)
            return;

        if (!IsGameWorldActive())
        {
            if (IsPaused)
                ClosePauseInstant();

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsPaused)
            {
                OpenPauseMenu();
                return;
            }

            if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
            {
                BackFromSettingsPanel();
                return;
            }

            ResumeGame();
        }
    }

    private bool IsGameWorldActive()
    {
        return gameWorld != null && gameWorld.activeInHierarchy;
    }

    public void OpenPauseMenu()
    {
        if (!IsGameWorldActive())
            return;

        IsPaused = true;

        _previousLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseRootPanel != null)
            pauseRootPanel.SetActive(true);

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(true);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        IsPaused = false;

        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisible;

        if (pauseRootPanel != null)
            pauseRootPanel.SetActive(false);

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(false);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);
    }

    public void OpenSettingsPanel()
    {
        if (!IsPaused)
            return;

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(false);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(true);
    }

    public void BackFromSettingsPanel()
    {
        if (!IsPaused)
            return;

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        if (_isLeavingOrQuitting)
            return;

        _isLeavingOrQuitting = true;

        ClosePauseInstant();

        if (mainMenuManager != null)
        {
            mainMenuManager.LeaveLobby();
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: MainMenuManager bulunamadı.");
        }

        _isLeavingOrQuitting = false;
    }

    public void QuitGame()
    {
        if (_isLeavingOrQuitting)
            return;

        _isLeavingOrQuitting = true;

        ClosePauseInstant();

        if (mainMenuManager != null)
        {
            mainMenuManager.LeaveLobby();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ClosePauseInstant()
    {
        IsPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseRootPanel != null)
            pauseRootPanel.SetActive(false);

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(false);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);
    }
}