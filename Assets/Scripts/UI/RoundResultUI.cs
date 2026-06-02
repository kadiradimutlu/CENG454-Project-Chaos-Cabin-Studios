using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoundResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private LobbyState lobbyState;
    private Graphic[] graphics;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (resultPanel == null)
            resultPanel = gameObject;

        if (resultText == null && resultPanel != null)
            resultText = resultPanel.GetComponentInChildren<TMP_Text>(true);

        CacheComponents();
        SetVisible(false);
    }

    private void OnEnable()
    {
        lobbyState = null;
        CacheComponents();
        SetVisible(false);
    }

    private void Update()
    {
        CacheLobbyState();

        if (lobbyState == null || !lobbyState.GameStarted || lobbyState.RoundStateValue != 3)
        {
            SetVisible(false);
            return;
        }

        if (resultText != null)
        {
            if (lobbyState.RoundWinnerValue == 1)
                resultText.text = "RUNNERS WIN";
            else if (lobbyState.RoundWinnerValue == 2)
                resultText.text = "TRAPPERS WIN";
            else
                resultText.text = "ROUND FINISHED";
        }

        SetVisible(true);
    }

    private void SetVisible(bool value)
    {
        if (resultPanel != null && !resultPanel.activeSelf)
            resultPanel.SetActive(true);

        CacheComponents();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = value ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (graphics == null)
            return;

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].enabled = value;
                graphics[i].raycastTarget = false;
            }
        }
    }

    private void CacheComponents()
    {
        if (resultPanel == null)
            return;

        graphics = resultPanel.GetComponentsInChildren<Graphic>(true);
        canvasGroup = resultPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = resultPanel.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void CacheLobbyState()
    {
        if (lobbyState != null)
            return;

        lobbyState = FindFirstObjectByType<LobbyState>();
    }
}
