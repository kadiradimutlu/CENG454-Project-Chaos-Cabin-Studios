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

    private void Awake()
    {
        if (resultPanel == null)
            resultPanel = gameObject;

        if (resultText == null && resultPanel != null)
            resultText = resultPanel.GetComponentInChildren<TMP_Text>(true);

        CacheGraphics();
        SetVisible(false);
    }

    private void OnEnable()
    {
        lobbyState = null;
        CacheGraphics();
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

        CacheGraphics();

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

    private void CacheGraphics()
    {
        if (resultPanel == null)
            return;

        graphics = resultPanel.GetComponentsInChildren<Graphic>(true);
    }

    private void CacheLobbyState()
    {
        if (lobbyState != null)
            return;

        lobbyState = FindFirstObjectByType<LobbyState>();
    }
}
