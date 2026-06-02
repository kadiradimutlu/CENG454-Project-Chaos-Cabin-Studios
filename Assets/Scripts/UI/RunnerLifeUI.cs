using Fusion;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RunnerLifeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text deathRightsText;
    [SerializeField] private GameObject contentRoot;

    private NetworkRunner runner;
    private RunnerLife observedLife;
    private CanvasGroup contentGroup;

    private void Awake()
    {
        if (deathRightsText == null)
            deathRightsText = GetComponent<TMP_Text>();

        if (deathRightsText == null)
            deathRightsText = GetComponentInChildren<TMP_Text>(true);

        if (contentRoot == null)
            contentRoot = gameObject;

        contentGroup = contentRoot.GetComponent<CanvasGroup>();
        if (contentGroup == null)
            contentGroup = contentRoot.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        observedLife = null;
        SetVisible(false);
    }

    private void Update()
    {
        if (observedLife == null)
            TryBindToLocalPlayer();

        UpdateText();
    }

    private void TryBindToLocalPlayer()
    {
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null)
            return;

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
        if (playerObject != null)
        {
            observedLife = playerObject.GetComponent<RunnerLife>();
            return;
        }

        RunnerLife[] allLives = FindObjectsByType<RunnerLife>(FindObjectsSortMode.None);
        for (int i = 0; i < allLives.Length; i++)
        {
            if (allLives[i] != null && allLives[i].Object != null && allLives[i].Object.HasInputAuthority)
            {
                observedLife = allLives[i];
                return;
            }
        }
    }

    private void UpdateText()
    {
        if (observedLife == null || !observedLife.IsRunnerPlayer)
        {
            SetVisible(false);
            return;
        }

        if (deathRightsText == null)
            return;

        deathRightsText.text = "Death Rights: " + observedLife.RemainingDeathRights;
        SetVisible(true);
    }

    private void SetVisible(bool value)
    {
        if (contentGroup != null)
        {
            contentGroup.alpha = value ? 1f : 0f;
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            return;
        }

        if (deathRightsText != null)
            deathRightsText.enabled = value;
    }
}
