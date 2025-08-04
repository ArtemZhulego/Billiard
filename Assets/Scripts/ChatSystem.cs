using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ChatSystem : MonoBehaviour
{
    [Header("Chat States")]
    [SerializeField] private RectTransform fullChatWindow;
    [SerializeField] private RectTransform minimizedChatWindow;
    [SerializeField] private Button expandButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private TMP_Text minimizedChatText;

    [Header("Chat UI")]
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private RectTransform chatContent;
    [SerializeField] private GameObject textMessagePrefab;
    [SerializeField] private GameObject imageMessagePrefab;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;

    [Header("Quick Access")]
    [SerializeField] private Toggle phrasesToggle;
    [SerializeField] private Toggle emojisToggle;
    [SerializeField] private ScrollRect phrasesScrollView;
    [SerializeField] private ScrollRect emojisScrollView;
    [SerializeField] private RectTransform phrasesContent;
    [SerializeField] private RectTransform emojisContent;
    [SerializeField] private GameObject phraseButtonPrefab;
    [SerializeField] private GameObject emojiButtonPrefab;

    [Header("Data")]
    [SerializeField] private string[] phrases;
    [SerializeField] private Sprite[] emojis;

    private readonly List<ChatMessage> _chatHistory = new List<ChatMessage>();
    private bool _isChatExpanded = false;

    private class ChatMessage
    {
        public string text;
        public Sprite image;
        public bool isFromPlayer;
    }

    private void Start()
    {
        InitializeChatWindow();
        ValidateReferences();
        SetupToggles();
        SetupButtons();
        InitializeContent();
        ForceToggleState();
    }

    private void InitializeChatWindow()
    {
        fullChatWindow.gameObject.SetActive(false);
        minimizedChatWindow.gameObject.SetActive(true);

        expandButton.onClick.AddListener(ExpandChat);
        minimizeButton.onClick.AddListener(MinimizeChat);
    }

    private void ExpandChat()
    {
        minimizedChatWindow.gameObject.SetActive(false);
        fullChatWindow.gameObject.SetActive(true);
        _isChatExpanded = true;
        StartCoroutine(ScrollToBottom());
    }

    private void MinimizeChat()
    {
        fullChatWindow.gameObject.SetActive(false);
        minimizedChatWindow.gameObject.SetActive(true);
        _isChatExpanded = false;
    }

    private void ValidateReferences()
    {
        if (phrasesScrollView is null || emojisScrollView is null ||
            phrasesContent is null || emojisContent is null ||
            phraseButtonPrefab is null || emojiButtonPrefab is null ||
            textMessagePrefab is null || imageMessagePrefab is null)
        {
            enabled = false;
        }
    }

    private void SetupToggles()
    {
        phrasesToggle.onValueChanged.RemoveAllListeners();
        emojisToggle.onValueChanged.RemoveAllListeners();

        phrasesToggle.onValueChanged.AddListener(TogglePhrases);
        emojisToggle.onValueChanged.AddListener(ToggleEmojis);
    }

    private void ForceToggleState()
    {
        phrasesToggle.isOn = true;
        emojisToggle.isOn = false;
        TogglePhrases(true);
        ToggleEmojis(false);
    }

    private void SetupButtons()
    {
        sendButton.onClick.RemoveAllListeners();
        sendButton.onClick.AddListener(SendMessage);

        if (closeButton is not null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    private void InitializeContent()
    {
        InitializePhrases();
        InitializeEmojis();
    }

    private void InitializePhrases()
    {
        ClearContent(phrasesContent);

        foreach (var phrase in phrases)
        {
            if (string.IsNullOrEmpty(phrase)) continue;
            CreatePhraseButton(phrase);
        }

        UpdateLayout(phrasesContent);
    }

    private void InitializeEmojis()
    {
        ClearContent(emojisContent);

        foreach (var emoji in emojis)
        {
            if (emoji is null) continue;
            CreateEmojiButton(emoji);
        }

        UpdateLayout(emojisContent);
    }

    private void ClearContent(Transform content)
    {
        foreach (Transform child in content)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    private void UpdateLayout(RectTransform content)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    private void CreatePhraseButton(string phrase)
    {
        var buttonObj = Instantiate(phraseButtonPrefab, phrasesContent);
        buttonObj.name = $"PhraseBtn_{phrase}";

        var textComponent = buttonObj.GetComponentInChildren<TMP_Text>();
        var buttonComponent = buttonObj.GetComponent<Button>();

        if (textComponent is null || buttonComponent is null)
        {
            DestroyImmediate(buttonObj);
            return;
        }

        textComponent.text = phrase;
        buttonComponent.onClick.AddListener(() => {
            SendTextMessage(phrase, true);
        });
    }

    private void CreateEmojiButton(Sprite emoji)
    {
        var buttonObj = Instantiate(emojiButtonPrefab, emojisContent);
        buttonObj.name = $"EmojiBtn_{emoji.name}";

        var imageComponent = buttonObj.GetComponent<Image>();
        var buttonComponent = buttonObj.GetComponent<Button>();

        if (imageComponent is null || buttonComponent is null)
        {
            DestroyImmediate(buttonObj);
            return;
        }

        imageComponent.sprite = emoji;
        buttonComponent.onClick.AddListener(() => {
            SendImageMessage(emoji, true);
        });
    }

    private void SendTextMessage(string text, bool isFromPlayer)
    {
        _chatHistory.Add(new ChatMessage { text = text, isFromPlayer = isFromPlayer });
        CreateTextMessage(text, isFromPlayer);
        UpdateMinimizedChat(text);
        StartCoroutine(ScrollToBottom());
    }

    private void SendImageMessage(Sprite image, bool isFromPlayer)
    {
        _chatHistory.Add(new ChatMessage { image = image, isFromPlayer = isFromPlayer });
        CreateImageMessage(image, isFromPlayer);
        UpdateMinimizedChat("[Sticker]");
        StartCoroutine(ScrollToBottom());
    }

    private void UpdateMinimizedChat(string message) =>
        minimizedChatText.text = message.Length > 20 ? message.Substring(0, 20) + "..." : message;

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(messageInput?.text)) return;

        var message = messageInput.text.Trim();
        SendTextMessage(message, true);
        ResetInputField();
    }

    public void AddReceivedMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        SendTextMessage(message, false);
    }

    public void AddReceivedSticker(Sprite sticker)
    {
        if (sticker is null) return;
        SendImageMessage(sticker, false);
    }

    private void CreateTextMessage(string text, bool isFromPlayer)
    {
        if (chatContent is null || textMessagePrefab is null) return;

        var messageObj = Instantiate(textMessagePrefab, chatContent);
        var textComponent = messageObj.GetComponent<TMP_Text>();

        if (textComponent is not null)
        {
            textComponent.text = (isFromPlayer ? "Вы: " : "") + text;
            textComponent.alignment = isFromPlayer ?
                TextAlignmentOptions.MidlineLeft :
                TextAlignmentOptions.MidlineRight;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
    }

    private void CreateImageMessage(Sprite image, bool isFromPlayer)
    {
        if (chatContent is null || imageMessagePrefab is null) return;

        var messageObj = Instantiate(imageMessagePrefab, chatContent);
        var imageComponent = messageObj.GetComponent<Image>();
        var layoutGroup = messageObj.GetComponent<HorizontalLayoutGroup>();

        if (imageComponent is not null)
        {
            imageComponent.sprite = image;
            imageComponent.preserveAspect = true;
        }

        if (layoutGroup is not null)
        {
            layoutGroup.childAlignment = isFromPlayer ?
                TextAnchor.MiddleLeft :
                TextAnchor.MiddleRight;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
    }

    private void ResetInputField()
    {
        messageInput.text = "";
        messageInput.ActivateInputField();
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        if (chatScrollRect is not null)
        {
            chatScrollRect.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
        }
    }

    public void TogglePhrases(bool isOn)
    {
        phrasesScrollView.gameObject.SetActive(isOn);
        if (isOn) emojisToggle.isOn = false;
    }

    public void ToggleEmojis(bool isOn)
    {
        emojisScrollView.gameObject.SetActive(isOn);
        if (isOn) phrasesToggle.isOn = false;
    }
}