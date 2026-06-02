using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChatUI : MonoBehaviour
{
    [Header("Chat UI")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatHistoryText;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Scroll Settings")]
    [SerializeField] private bool alwaysAutoScrollToBottom = true;

    private LobbyState _lobbyState;
    private int _lastSeenChatVersion = -1;
    private float _findTimer;
    private bool _shouldScrollToBottom;

    private void Awake()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(SendCurrentMessage);

        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnInputSubmit);

        ClearChatView();
    }

    private void OnEnable()
    {
        _lobbyState = null;
        _lastSeenChatVersion = -1;
        _findTimer = 0f;
        _shouldScrollToBottom = true;

        TryFindLobbyState();
        RefreshChat(true);
    }

    private void OnDisable()
    {
        _lobbyState = null;
        _lastSeenChatVersion = -1;
        _shouldScrollToBottom = false;
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(SendCurrentMessage);

        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnInputSubmit);
    }

    private void Update()
    {
        if (_lobbyState == null)
        {
            _findTimer += Time.unscaledDeltaTime;

            if (_findTimer >= 0.25f)
            {
                _findTimer = 0f;
                TryFindLobbyState();
            }
        }

        RefreshChat(false);

        if (_shouldScrollToBottom)
        {
            _shouldScrollToBottom = false;
            ScrollToBottom();
        }
    }

    private void LateUpdate()
    {
        if (_shouldScrollToBottom)
        {
            _shouldScrollToBottom = false;
            ScrollToBottom();
        }
    }

    private void TryFindLobbyState()
    {
        _lobbyState = FindObjectOfType<LobbyState>();
    }

    private void OnInputSubmit(string value)
    {
        SendCurrentMessage();
    }

    public void SendCurrentMessage()
    {
        if (chatInputField == null)
            return;

        string message = chatInputField.text;

        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_lobbyState == null)
            TryFindLobbyState();

        if (_lobbyState == null)
        {
            Debug.LogWarning("LobbyChatUI: LobbyState bulunamadı, mesaj gönderilemedi.");
            return;
        }

        try
        {
            _lobbyState.RPC_SendChatMessage(message);
        }
        catch (InvalidOperationException)
        {
            _lobbyState = null;
            Debug.LogWarning("LobbyChatUI: LobbyState hazır değil, mesaj gönderilemedi.");
            return;
        }

        chatInputField.text = string.Empty;
        chatInputField.ActivateInputField();

        _shouldScrollToBottom = true;
    }

    private void RefreshChat(bool force)
    {
        if (_lobbyState == null)
        {
            if (force)
                ClearChatView();

            return;
        }

        int chatVersion;

        try
        {
            chatVersion = _lobbyState.ChatMessageVersion;
        }
        catch (InvalidOperationException)
        {
            _lobbyState = null;

            if (force)
                ClearChatView();

            return;
        }

        if (!force && _lastSeenChatVersion == chatVersion)
            return;

        _lastSeenChatVersion = chatVersion;

        if (chatHistoryText != null)
        {
            try
            {
                chatHistoryText.text = _lobbyState.GetChatLog();
            }
            catch (InvalidOperationException)
            {
                _lobbyState = null;
                ClearChatView();
                return;
            }
        }

        if (alwaysAutoScrollToBottom || force)
            _shouldScrollToBottom = true;
    }

    private void ClearChatView()
    {
        if (chatHistoryText != null)
            chatHistoryText.text = string.Empty;
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        scrollRect.verticalNormalizedPosition = 0f;

        Canvas.ForceUpdateCanvases();
    }
}
