using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Level Manager Prefab")]
    public GameObject levelManagerPrefab; 

    [Header("First Level Settings")]
    public string firstLevelName = "Level1";
    public string highScoresSceneName = "High-Scores";
    public string aboutSceneName = "About";

    [Header("UI References")]
    public Button startButton;
    public Button highScoresButton;
    public Button quitButton;
    public Button backButton;
    public Button musicToggleButton;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject highScoresPanel;

    [Header("High Scores Display")]
    public Transform highScoreContainer;
    public GameObject highScoreEntryPrefab;
    public TextMeshProUGUI noScoresText;

    [Header("Audio Settings")]
    public AudioSource backgroundMusicSource;
    public AudioClip backgroundMusicClip;
    public Sprite musicOnIcon;
    public Sprite musicOffIcon;

    private bool isMusicEnabled = true;
    private const string MUSIC_PREF_KEY = "BackgroundMusicEnabled";

    void Start()
    {
        isMusicEnabled = PlayerPrefs.GetInt(MUSIC_PREF_KEY, 1) == 1;

        SetupBackgroundMusic();

        if (ScoreManager.Instance == null)
        {
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManagerObj.AddComponent<ScoreManager>();
        }

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
        else
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                if (btn.name.ToLower().Contains("start") || btn.name.ToLower().Contains("play"))
                {
                    btn.onClick.AddListener(StartGame);
                    break;
                }
            }
        }

        if (highScoresButton != null)
            highScoresButton.onClick.AddListener(ShowHighScores);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        else
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                if (btn.name.ToLower().Contains("quit") || btn.name.ToLower().Contains("exit"))
                {
                    btn.onClick.AddListener(QuitGame);
                    break;
                }
            }
        }

        if (backButton != null)
            backButton.onClick.AddListener(BackToMainMenu);

        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.AddListener(ToggleMusic);
            UpdateMusicButtonVisual();
        }
        else
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                if (btn.name.ToLower().Contains("music") || btn.name.ToLower().Contains("sound"))
                {
                    musicToggleButton = btn;
                    btn.onClick.AddListener(ToggleMusic);
                    UpdateMusicButtonVisual();
                    break;
                }
            }
        }

        ShowMainMenu();
    }

    public void SetupBackgroundMusic()
    {
        if (MusicManager.Instance == null)
        {
            GameObject musicManagerObj = new GameObject("MusicManager");
            MusicManager musicManager = musicManagerObj.AddComponent<MusicManager>();

            if (backgroundMusicClip != null)
            {
                musicManager.backgroundMusic = backgroundMusicClip;
                musicManager.PlayMusic();
            }
        }

        if (MusicManager.Instance != null)
        {
            isMusicEnabled = MusicManager.Instance.IsMusicEnabled();
        }
    }

    public void ToggleMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ToggleMusic();
            isMusicEnabled = MusicManager.Instance.IsMusicEnabled();
        }
        else
        {
            isMusicEnabled = !isMusicEnabled;
            PlayerPrefs.SetInt(MUSIC_PREF_KEY, isMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        UpdateMusicButtonVisual();
    }

    void UpdateMusicButtonVisual()
    {
        if (musicToggleButton == null) return;

        Image buttonImage = musicToggleButton.GetComponent<Image>();
        if (buttonImage != null && musicOnIcon != null && musicOffIcon != null)
        {
            buttonImage.sprite = isMusicEnabled ? musicOnIcon : musicOffIcon;
        }

        TextMeshProUGUI buttonText = musicToggleButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = isMusicEnabled ? "Music: ON" : "Music: OFF";
        }
        else
        {
            Text legacyText = musicToggleButton.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text = isMusicEnabled ? "Music: ON" : "Music: OFF";
            }
        }
    }

    public void StartGame()
    {
        if (LevelManager.Instance != null)
        {
            Destroy(LevelManager.Instance.gameObject);
        }

        LevelManager.ResetPersistentData();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetCurrentScore();
        }

        if (levelManagerPrefab != null)
        {
            Instantiate(levelManagerPrefab);
        }

        SceneManager.LoadScene(firstLevelName);
    }

    public void ShowHighScores()
    {
        SceneManager.LoadScene(highScoresSceneName);
    }

    public void AboutScreen()
    {
        SceneManager.LoadScene(aboutSceneName);
    }

    void BackToMainMenu()
    {
        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (highScoresPanel != null)
            highScoresPanel.SetActive(false);
    }

    public void DisplayHighScores()
    {
        if (highScoreContainer == null) return;

        foreach (Transform child in highScoreContainer)
        {
            Destroy(child.gameObject);
        }

        var highScores = ScoreManager.Instance != null ?
            ScoreManager.Instance.GetHighScores() :
            new System.Collections.Generic.List<ScoreEntry>();

        if (highScores.Count == 0)
        {
            if (noScoresText != null)
            {
                noScoresText.gameObject.SetActive(true);
                noScoresText.text = "No high scores yet!\nBe the first to set one!";
            }
            return;
        }

        if (noScoresText != null)
            noScoresText.gameObject.SetActive(false);

        if (highScoreEntryPrefab == null)
            CreateHighScoreEntryPrefab();

        int rank = 1;
        foreach (var score in highScores)
        {
            GameObject entry = Instantiate(highScoreEntryPrefab, highScoreContainer);
            entry.SetActive(true);

            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                texts[0].text = $"#{rank}";
                texts[1].text = score.playerName;
                texts[2].text = score.score.ToString("N0");
            }

            rank++;
        }
    }

    void CreateHighScoresPanel()
    {
        GameObject panel = new GameObject("HighScoresPanel");
        panel.transform.SetParent(transform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform);
        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "HIGH SCORES";
        titleText.fontSize = 36;
        titleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.9f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(400, 50);

        GameObject backBtn = new GameObject("BackButton");
        backBtn.transform.SetParent(panel.transform);
        Button back = backBtn.AddComponent<Button>();
        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = Color.white;

        GameObject backText = new GameObject("Text");
        backText.transform.SetParent(backBtn.transform);
        TextMeshProUGUI backTxt = backText.AddComponent<TextMeshProUGUI>();
        backTxt.text = "Back";
        backTxt.fontSize = 24;
        backTxt.alignment = TextAlignmentOptions.Center;
        backTxt.color = Color.black;

        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.1f);
        backRect.anchorMax = new Vector2(0.5f, 0.1f);
        backRect.anchoredPosition = Vector2.zero;
        backRect.sizeDelta = new Vector2(200, 50);

        back.onClick.AddListener(BackToMainMenu);
        backButton = back;

        highScoresPanel = panel;
        highScoresPanel.SetActive(false);
    }


    void CreateHighScoreEntryPrefab()
    {
        GameObject prefab = new GameObject("HighScoreEntry");

        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 30);

        HorizontalLayoutGroup layout = prefab.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(20, 20, 5, 5);

        GameObject rankObj = new GameObject("Rank");
        rankObj.transform.SetParent(prefab.transform);
        TextMeshProUGUI rankText = rankObj.AddComponent<TextMeshProUGUI>();
        rankText.text = "#1";
        rankText.fontSize = 20;

        LayoutElement rankLayout = rankObj.AddComponent<LayoutElement>();
        rankLayout.preferredWidth = 50;

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(prefab.transform);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Player";
        nameText.fontSize = 18;

        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;

        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(prefab.transform);
        TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "0";
        scoreText.fontSize = 20;

        LayoutElement scoreLayout = scoreObj.AddComponent<LayoutElement>();
        scoreLayout.preferredWidth = 100;

        highScoreEntryPrefab = prefab;
        highScoreEntryPrefab.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnDestroy()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            // backgroundMusicSource.Stop();
        }
    }
}