using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ScoreEntry
{
    public string playerName;
    public int score;

    public ScoreEntry(string name, int score)
    {
        this.playerName = name;
        this.score = score;
    }
}

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager instance;
    public static ScoreManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ScoreManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ScoreManager");
                    instance = go.AddComponent<ScoreManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("Score Settings")]
    public int maxScoresToSave = 1000;

    private List<ScoreEntry> highScores = new List<ScoreEntry>();
    private int currentScore = 0;
    private const string HIGHSCORES_KEY = "SpaceShooterHighScores";
    private const string LAST_SCORE_KEY = "LastScore";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadScores();
    }

    public void AddScore(int points)
    {
        currentScore += points;
    }

    public void SetScore(int score)
    {
        currentScore = score;
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public void ResetCurrentScore()
    {
        currentScore = 0;
    }

    public void SaveScore(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            playerName = "Anonymous";

        ScoreEntry newEntry = new ScoreEntry(playerName, currentScore);
        highScores.Add(newEntry);

        highScores = highScores.OrderByDescending(s => s.score).ToList();

        if (highScores.Count > maxScoresToSave)
        {
            highScores = highScores.Take(maxScoresToSave).ToList();
        }

        SaveScoresToPrefs();

        PlayerPrefs.SetInt(LAST_SCORE_KEY, currentScore);
        PlayerPrefs.Save();
    }

    public List<ScoreEntry> GetHighScores()
    {
        return new List<ScoreEntry>(highScores);
    }

    public int GetLastScore()
    {
        return PlayerPrefs.GetInt(LAST_SCORE_KEY, 0);
    }

    public bool IsHighScore(int score)
    {
        if (highScores.Count < maxScoresToSave)
            return true;

        return score > highScores[highScores.Count - 1].score;
    }

    private void SaveScoresToPrefs()
    {
        string json = JsonUtility.ToJson(new SerializableList<ScoreEntry>(highScores));
        PlayerPrefs.SetString(HIGHSCORES_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadScores()
    {
        if (PlayerPrefs.HasKey(HIGHSCORES_KEY))
        {
            string json = PlayerPrefs.GetString(HIGHSCORES_KEY);
            SerializableList<ScoreEntry> loadedScores = JsonUtility.FromJson<SerializableList<ScoreEntry>>(json);
            if (loadedScores != null && loadedScores.items != null)
            {
                highScores = loadedScores.items;
            }
        }
    }

    public void ClearAllScores()
    {
        highScores.Clear();
        PlayerPrefs.DeleteKey(HIGHSCORES_KEY);
        PlayerPrefs.DeleteKey(LAST_SCORE_KEY);
        PlayerPrefs.Save();
    }

    // helper class for JSON serialization of lists
    [System.Serializable]
    private class SerializableList<T>
    {
        public List<T> items;

        public SerializableList(List<T> list)
        {
            items = list;
        }
    }
}