using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject chatPanel;
    public ScrollRect chatScrollRect;
    public Transform chatContent;
    public TMP_InputField inputField;
    public Button sendButton;
    public Button toggleButton;
    public GameObject messagePrefab;

    [Header("Settings")]
    public int maxMessages = 50;
    public Color playerMessageColor = Color.white;
    public Color budyMessageColor = Color.cyan;
    public KeyCode toggleKey = KeyCode.Return;

    private GeminiAIManager geminiManager;
    private BudyAI budyAI;
    public bool isChatVisible = false;
    private bool isWaitingForResponse = false;
    private bool wasGamePausedBefore = false;

    void Start()
    {
        geminiManager = FindObjectOfType<GeminiAIManager>();
        if (geminiManager == null)
        {
            Debug.LogError("GeminiAIManager not found! Please add it to the scene.");
        }

        GameObject budy = GameObject.Find("Budy");
        if (budy != null)
        {
            budyAI = budy.GetComponent<BudyAI>();
        }

        SetupUI();
        SetChatVisibility(false);

        // initial message
        AddMessage("Budy", "Hello! Type a message to chat with me!", budyMessageColor);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleChat();
        }

        if (isChatVisible && inputField.isFocused && Input.GetKeyDown(KeyCode.Return) && !Input.GetKey(KeyCode.LeftShift))
        {
            SendMessage();
        }
    }

    void SetupUI()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendMessage);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleChat);
        }

        if (messagePrefab == null)
        {
            CreateMessagePrefab();
        }
    }

    void CreateMessagePrefab()
    {
        GameObject prefab = new GameObject("MessagePrefab");

        Image bgImage = prefab.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.3f);

        // add text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(prefab.transform);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Sample message";
        text.fontSize = 14;
        text.color = Color.white;
        text.enableWordWrapping = true;

        RectTransform prefabRect = prefab.GetComponent<RectTransform>();
        RectTransform textRect = textObj.GetComponent<RectTransform>();

        prefabRect.anchorMin = new Vector2(0, 1);
        prefabRect.anchorMax = new Vector2(1, 1);
        prefabRect.pivot = new Vector2(0, 1);


        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        LayoutElement layoutElement = prefab.AddComponent<LayoutElement>();
        layoutElement.minHeight = 30;
        layoutElement.flexibleHeight = -1;


        ContentSizeFitter sizeFitter = prefab.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        messagePrefab = prefab;
        messagePrefab.SetActive(false);
    }

    public void ToggleChat()
    {
        SetChatVisibility(!isChatVisible);
    }

    void SetChatVisibility(bool visible)
    {
        isChatVisible = visible;
        if (chatPanel != null)
        {
            chatPanel.SetActive(visible);
        }

        // handle pausing the game
        if (visible)
        {
            wasGamePausedBefore = Time.timeScale == 0f;
            Time.timeScale = 0f;

            DisablePlayerInput(true);

            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }
        else
        {
            if (!wasGamePausedBefore)
            {
                Time.timeScale = 1f;
            }

            DisablePlayerInput(false);
        }

        if (toggleButton != null)
        {
            TextMeshProUGUI buttonText = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = visible ? "Resume Game" : "Chat with Budy";
            }
        }
    }

    public void SendMessage()
    {
        Debug.Log("SendMessage called");
        Debug.Log($"InputField null: {inputField == null}");
        Debug.Log($"InputField text: '{inputField?.text}'");
        Debug.Log($"IsWaitingForResponse: {isWaitingForResponse}");
        if (inputField == null || string.IsNullOrEmpty(inputField.text.Trim()) || isWaitingForResponse)
            return;

        string message = inputField.text.Trim();
        inputField.text = "";

        AddMessage("You", message, playerMessageColor);

        // update mood based on message
        if (budyAI != null)
        {
            budyAI.UpdateMoodFromChat(message);
        }

        GameObject typingMessage = AddMessage("Budy", "Budy is thinking...", budyMessageColor);
        isWaitingForResponse = true;

        if (sendButton != null)
        {
            sendButton.interactable = false;
        }

        // context trimis catre gemini
        if (geminiManager != null)
        {
            string messageWithMood = message;
            if (budyAI != null)
            {
                messageWithMood += $"\n[Current mood: {budyAI.GetCurrentMood()}]";
            }

            geminiManager.SendMessageWithContext(messageWithMood,
                (response) => OnAIResponse(response, typingMessage),
                (error) => OnAIError(error, typingMessage));
        }
        else
        {
            OnAIError("Gemini AI Manager not found!", typingMessage);
        }

        StartCoroutine(RefocusInput());
    }

    void OnAIResponse(string response, GameObject typingMessage)
    {
        if (typingMessage != null)
        {
            Destroy(typingMessage);
        }

        AddMessage("Budy", response, budyMessageColor);

        if (budyAI != null)
        {
            budyAI.UpdateMoodFromChat(response);
        }

        isWaitingForResponse = false;

        if (sendButton != null)
        {
            sendButton.interactable = true;
        }
    }

    void OnAIError(string error, GameObject typingMessage)
    {
        if (typingMessage != null)
        {
            Destroy(typingMessage);
        }

        AddMessage("Budy", $"Sorry, I'm having trouble connecting right now. Error: {error}", Color.red);

        isWaitingForResponse = false;

        if (sendButton != null)
        {
            sendButton.interactable = true;
        }
    }

    GameObject AddMessage(string sender, string message, Color color)
    {
        if (chatContent == null || messagePrefab == null)
            return null;

        GameObject messageObj = Instantiate(messagePrefab, chatContent);
        messageObj.SetActive(true);

        TextMeshProUGUI textComponent = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = $"<b>{sender}:</b> {message}";
            textComponent.color = color;
        }

        Image bgImage = messageObj.GetComponent<Image>();
        if (bgImage != null)
        {
            if (sender == "You")
            {
                bgImage.color = new Color(0.2f, 0.3f, 0.8f, 0.3f); 
            }
            else if (sender == "Budy")
            {
                if (budyAI != null)
                {
                    switch (budyAI.GetCurrentMood())
                    {
                        case BudyAI.BudyMood.Calm:
                            bgImage.color = new Color(0.2f, 0.8f, 0.3f, 0.3f); // green
                            break;
                        case BudyAI.BudyMood.Aggressive:
                            bgImage.color = new Color(0.8f, 0.2f, 0.2f, 0.3f); // red
                            break;
                        case BudyAI.BudyMood.Scared:
                            bgImage.color = new Color(0.8f, 0.8f, 0.2f, 0.3f); // yellow
                            break;
                        case BudyAI.BudyMood.Supportive:
                            bgImage.color = new Color(0.2f, 0.8f, 0.8f, 0.3f); // cyan
                            break;
                    }
                }
                else
                {
                    bgImage.color = new Color(0.2f, 0.8f, 0.3f, 0.3f); // default
                }
            }
            else
            {
                bgImage.color = new Color(0.8f, 0.2f, 0.2f, 0.3f); // for errors
            }
        }

        if (chatContent.childCount > maxMessages)
        {
            Destroy(chatContent.GetChild(0).gameObject);
        }

        StartCoroutine(ScrollToBottom());

        return messageObj;
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    IEnumerator RefocusInput()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        if (isChatVisible && inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    void DisablePlayerInput(bool disable)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            MoveNAVA playerMovement = player.GetComponent<MoveNAVA>();
            if (playerMovement != null)
            {
                playerMovement.enabled = !disable;
            }

            MonoBehaviour[] playerScripts = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in playerScripts)
            {
                if (script.GetType().Name.Contains("Input") ||
                    script.GetType().Name.Contains("Control") ||
                    script.GetType().Name.Contains("Shoot"))
                {
                    script.enabled = !disable;
                }
            }
        }

        if (disable)
        {
            AsteroidSpawner asteroidSpawner = FindObjectOfType<AsteroidSpawner>();
            if (asteroidSpawner != null)
            {
                asteroidSpawner.SetSpawning(false);
            }
        }
        else
        {
            AsteroidSpawner asteroidSpawner = FindObjectOfType<AsteroidSpawner>();
            if (asteroidSpawner != null)
            {
                asteroidSpawner.SetSpawning(true);
            }
        }
    }
}