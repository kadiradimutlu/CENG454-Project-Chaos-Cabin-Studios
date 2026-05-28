using System.Collections.Generic;
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

    private readonly List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();

    private bool _isLeavingOrQuitting;
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;
    private Canvas _pauseCanvas;
    private GraphicRaycaster _pauseRaycaster;
    private CanvasGroup _pauseCanvasGroup;

    private void Awake()
    {
        if (mainMenuManager == null)
            mainMenuManager = FindFirstObjectByType<MainMenuManager>();

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

        PreparePauseInput();
        DisableOtherRaycasters();
    }

    public void ResumeGame()
    {
        IsPaused = false;

        RestoreRaycasters();
        DisablePauseInput();

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

        PreparePauseInput();
    }

    public void BackFromSettingsPanel()
    {
        if (!IsPaused)
            return;

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(true);

        PreparePauseInput();
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
            mainMenuManager.LeaveLobby();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ClosePauseInstant()
    {
        IsPaused = false;

        RestoreRaycasters();
        DisablePauseInput();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseRootPanel != null)
            pauseRootPanel.SetActive(false);

        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(false);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);
    }

    private void PreparePauseInput()
    {
        if (pauseRootPanel == null)
            return;

        pauseRootPanel.transform.SetAsLastSibling();

        if (_pauseCanvas == null)
            _pauseCanvas = pauseRootPanel.GetComponent<Canvas>();

        if (_pauseCanvas == null)
            _pauseCanvas = pauseRootPanel.AddComponent<Canvas>();

        _pauseCanvas.overrideSorting = true;
        _pauseCanvas.sortingOrder = 10000;

        if (_pauseRaycaster == null)
            _pauseRaycaster = pauseRootPanel.GetComponent<GraphicRaycaster>();

        if (_pauseRaycaster == null)
            _pauseRaycaster = pauseRootPanel.AddComponent<GraphicRaycaster>();

        _pauseRaycaster.enabled = true;

        if (_pauseCanvasGroup == null)
            _pauseCanvasGroup = pauseRootPanel.GetComponent<CanvasGroup>();

        if (_pauseCanvasGroup == null)
            _pauseCanvasGroup = pauseRootPanel.AddComponent<CanvasGroup>();

        _pauseCanvasGroup.alpha = 1f;
        _pauseCanvasGroup.interactable = true;
        _pauseCanvasGroup.blocksRaycasts = true;
    }

    private void DisablePauseInput()
    {
        if (_pauseCanvasGroup != null)
        {
            _pauseCanvasGroup.interactable = false;
            _pauseCanvasGroup.blocksRaycasts = false;
        }
    }

    private void DisableOtherRaycasters()
    {
        disabledRaycasters.Clear();

        GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < raycasters.Length; i++)
        {
            GraphicRaycaster raycaster = raycasters[i];

            if (raycaster == null || !raycaster.enabled)
                continue;

            if (IsPauseObject(raycaster.transform))
                continue;

            raycaster.enabled = false;
            disabledRaycasters.Add(raycaster);
        }
    }

    private void RestoreRaycasters()
    {
        for (int i = 0; i < disabledRaycasters.Count; i++)
        {
            if (disabledRaycasters[i] != null)
                disabledRaycasters[i].enabled = true;
        }

        disabledRaycasters.Clear();
    }

    private bool IsPauseObject(Transform target)
    {
        if (target == null)
            return false;

        if (IsSameOrChild(target, pauseRootPanel))
            return true;

        if (IsSameOrChild(target, pauseMainPanel))
            return true;

        if (IsSameOrChild(target, pauseSettingsPanel))
            return true;

        return false;
    }

    private bool IsSameOrChild(Transform target, GameObject root)
    {
        if (root == null)
            return false;

        Transform rootTransform = root.transform;
        return target == rootTransform || target.IsChildOf(rootTransform);
    }
}
