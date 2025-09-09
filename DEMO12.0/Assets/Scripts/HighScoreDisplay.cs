using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class HighScoresDisplay : MonoBehaviour
{
    [Header("Score Display Settings")]
    public Transform scoresParent; // Parent object for all score texts (can be the Canvas)
    public float startY = 100f; // Y position of first score entry
    public float entrySpacing = 50f; // Space between each score entry
    public float rankX = -250f; // X position for rank numbers
    public float nameX = 0f; // X position for names
    public float scoreX = 250f; // X position for scores

    [Header("Text Settings")]
    public Font textFont; // Optional: custom font
    public int fontSize = 32;
    public Color textColor = Color.white;

    [Header("Navigation")]
    public Button backButton; // Your back button
    public string mainMenuSceneName = "MainMenu";

    private List<GameObject> scoreEntries = new List<GameObject>();

    void Start()
    {
        // Setup back button if assigned
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));
        }

        // Display the scores
        DisplayHighScores();
    }

    void DisplayHighScores()
    {
        // Get high scores from ScoreManager
        List<ScoreEntry> highScores = null;
        if (ScoreManager.Instance != null)
        {
            highScores = ScoreManager.Instance.GetHighScores();
        }

        if (highScores == null || highScores.Count == 0)
        {
            // Create "No scores" message
            CreateText("No high scores yet!", 0, 0, 40, textColor);
            return;
        }

        // Create score entries
        int rank = 1;
        float currentY = startY;

        foreach (ScoreEntry score in highScores)
        {
            // Create rank text (1, 2, 3, etc.)
            GameObject rankText = CreateText(rank.ToString(), rankX, currentY, fontSize, GetRankColor(rank));
            scoreEntries.Add(rankText);

            // Create name text
            GameObject nameText = CreateText(score.playerName.ToUpper(), nameX, currentY, fontSize, textColor);
            scoreEntries.Add(nameText);

            // Create score text
            GameObject scoreText = CreateText(score.score.ToString("N0"), scoreX, currentY, fontSize, textColor);
            scoreEntries.Add(scoreText);

            currentY -= entrySpacing;
            rank++;

            // Limit to 10 entries to fit on screen
            if (rank > 10) break;
        }
    }

    GameObject CreateText(string textContent, float x, float y, int size, Color color)
    {
        // Create new GameObject
        GameObject textObj = new GameObject("ScoreText_" + textContent);
        textObj.transform.SetParent(scoresParent != null ? scoresParent : transform);

        // Add and configure TextMeshPro
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = textContent;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;


        // Set position
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = new Vector2(300, 50);

        return textObj;
    }

    Color GetRankColor(int rank)
    {
        switch (rank)
        {
            case 1: return new Color(1f, 0.84f, 0f);
            case 2: return new Color(0.75f, 0.75f, 0.75f);
            case 3: return new Color(0.8f, 0.5f, 0.2f);
            default: return textColor;
        }
    }

    void OnDestroy()
    {
        foreach (GameObject entry in scoreEntries)
        {
            if (entry != null) Destroy(entry);
        }
    }
}