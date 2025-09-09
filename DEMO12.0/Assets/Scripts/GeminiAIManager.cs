using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GeminiAIManager : MonoBehaviour
{
    [Header("Gemini Configuration")]
    [SerializeField] private string apiKey = "AIzaSyB2CPYktPLihgSSAq2pA5gfQnO2s_0CAok";
    [SerializeField] private string model = "gemini-1.5-flash";

    [Header("Character Setup")]
    [SerializeField] private string systemPrompt = @"You are Budy, a helpful AI companion in a space shooter game. You're friendly, encouraging, and provide gameplay tips. Keep responses short and conversational. You help the player during their space adventure against aliens and asteroids.

Your mood changes based on the game situation and player messages:
- When the player uses aggressive words (attack, fight, destroy, kill, shoot), you become more aggressive and encourage combat
- When the player expresses fear or asks for help (scared, help, run, hide), you become worried and suggest defensive strategies
- When the player mentions needing health or healing, you become supportive and caring
- When the player is calm or encouraging (good job, amazing, nice), you remain calm and cheerful

Respond according to your current mood but always stay helpful and on the player's side.";

    private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    private BudyAI budyAI;

    [System.Serializable]
    public class ChatMessage
    {
        public string role; // "user" or "model"
        public string text;

        public ChatMessage(string role, string text)
        {
            this.role = role;
            this.text = text;
        }
    }

    [System.Serializable]
    public class GeminiRequest
    {
        public Content[] contents;
        public GenerationConfig generationConfig;

        public GeminiRequest(Content[] contents)
        {
            this.contents = contents;
            this.generationConfig = new GenerationConfig();
        }
    }

    [System.Serializable]
    public class Content
    {
        public Part[] parts;

        public Content(string text)
        {
            this.parts = new Part[] { new Part(text) };
        }
    }

    [System.Serializable]
    public class Part
    {
        public string text;

        public Part(string text)
        {
            this.text = text;
        }
    }

    [System.Serializable]
    public class GenerationConfig
    {
        public int maxOutputTokens = 150;
        public float temperature = 0.7f;
    }

    [System.Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    public class Candidate
    {
        public Content content;
    }

    private void Start()
    { 
        GameObject budy = GameObject.Find("Budy");
        if (budy != null)
        {
            budyAI = budy.GetComponent<BudyAI>();
        }

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            conversationHistory.Add(new ChatMessage("user", systemPrompt));
            conversationHistory.Add(new ChatMessage("model", "Hello friend! Ready for some alien hunting? Ask me anything about the game or just chat!"));
        }
    }

    public void SendMessage(string userMessage, System.Action<string> onResponse, System.Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(userMessage.Trim()))
        {
            onError?.Invoke("Please enter a message!");
            return;
        }

        if (string.IsNullOrEmpty(apiKey) || apiKey == "AIzaSyB2CPYktPLihgSSAq2pA5gfQnO2s_0CAok")
        {
            onError?.Invoke("Please set your Gemini API key in the GeminiAIManager!");
            return;
        }

        StartCoroutine(SendMessageCoroutine(userMessage, onResponse, onError));
    }

    private IEnumerator SendMessageCoroutine(string userMessage, System.Action<string> onResponse, System.Action<string> onError)
    {
        conversationHistory.Add(new ChatMessage("user", userMessage));

        string moodContext = "";
        if (budyAI != null)
        {
            BudyAI.BudyMood currentMood = budyAI.GetCurrentMood();
            moodContext = $"\n\n[Current mood: {currentMood}. ";

            switch (currentMood)
            {
                case BudyAI.BudyMood.Aggressive:
                    moodContext += "You are feeling aggressive and ready for combat. Encourage the player to fight!]";
                    break;
                case BudyAI.BudyMood.Scared:
                    moodContext += "You are feeling scared and worried. Suggest defensive strategies and caution!]";
                    break;
                case BudyAI.BudyMood.Supportive:
                    moodContext += "You are in supportive mode, focused on helping and healing the player!]";
                    break;
                case BudyAI.BudyMood.Calm:
                    moodContext += "You are feeling calm and collected. Be cheerful and encouraging!]";
                    break;
            }
        }

        string fullContext = systemPrompt + moodContext + "\n\nConversation:\n";

        int startIndex = Mathf.Max(0, conversationHistory.Count - 6);
        for (int i = startIndex; i < conversationHistory.Count; i++)
        {
            if (conversationHistory[i].role == "user")
            {
                fullContext += "Human: " + conversationHistory[i].text + "\n";
            }
            else
            {
                fullContext += "Budy: " + conversationHistory[i].text + "\n";
            }
        }

        fullContext += "Budy: ";

        // creating request
        GeminiRequest request = new GeminiRequest(new Content[] { new Content(fullContext) });
        string jsonRequest = JsonConvert.SerializeObject(request);

        // HTTP request
        string url = $"{GEMINI_API_URL}?key={apiKey}";

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = webRequest.downloadHandler.text;
                    GeminiResponse response = JsonConvert.DeserializeObject<GeminiResponse>(responseText);

                    if (response.candidates != null && response.candidates.Length > 0 &&
                        response.candidates[0].content != null && response.candidates[0].content.parts != null &&
                        response.candidates[0].content.parts.Length > 0)
                    {
                        string aiResponse = response.candidates[0].content.parts[0].text;

                        // add ai response to history
                        conversationHistory.Add(new ChatMessage("model", aiResponse));

                        onResponse?.Invoke(aiResponse);
                    }
                    else
                    {
                        onError?.Invoke("Invalid response format from Gemini API");
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Error parsing response: {e.Message}");
                }
            }
            else
            {
                string errorMsg = $"API Error: {webRequest.error}\nResponse: {webRequest.downloadHandler.text}";
                onError?.Invoke(errorMsg);
            }
        }
    }

    public void ClearConversation()
    {
        conversationHistory.Clear();
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            conversationHistory.Add(new ChatMessage("user", systemPrompt));
            conversationHistory.Add(new ChatMessage("model", "Hello! I'm Budy, your space companion! Ready for some alien hunting?"));
        }
    }

    // get game context with mood
    public string GetGameContext()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        string context = "";

        if (gameManager != null)
        {
            context = $"Player health: {gameManager.playerCurrentHealth}/{gameManager.playerMaxHealth}. " +
                     $"Game over: {gameManager.IsGameOver()}. " +
                     $"Level 3 (boss level): {gameManager.isLevel3}.";
        }

        if (budyAI != null)
        {
            context += $" Budy mood: {budyAI.GetCurrentMood()}.";
        }

        return context;
    }

    public void SendMessageWithContext(string userMessage, System.Action<string> onResponse, System.Action<string> onError = null)
    {
        string cleanMessage = userMessage;
        if (userMessage.Contains("[Current mood:"))
        {
            int startIndex = userMessage.IndexOf("[Current mood:");
            cleanMessage = userMessage.Substring(0, startIndex).Trim();
        }

        string messageWithContext = $"{cleanMessage}\n\nGame context: {GetGameContext()}";
        SendMessage(messageWithContext, onResponse, onError);
    }
}