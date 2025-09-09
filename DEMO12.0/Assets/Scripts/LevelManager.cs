using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    public float levelDuration = 60f; 
    public string[] levelSceneNames = { "Level1", "Level2", "Level3" };
    private int currentLevelIndex = 0;

    [Header("Transition Animation")]
    public Animator transitionAnimator; 
    public float transitionDuration = 1f; 

    [Header("Timer UI")]
    public GameObject timerPanel;
    public TextMeshProUGUI timerText;
    public Text legacyTimerText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip warningSound;

    private static int persistentScore = 0;
    private static int persistentHealth = -1; 
    private static int persistentMaxHealth = 5;

    private float timeRemaining;
    private bool isTransitioning = false;
    private bool timerActive = true;

    public static LevelManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        InitializeLevel();

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Level1" || currentScene == "Level2")
        {
            if (timerPanel == null)
            {
                CreateTimerUI();
            }
        }

        StartCoroutine(PlayStartAnimation());
    }

    void Update()
    {
        if (!isTransitioning && timerActive)
        {
            UpdateTimer();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            ForceNextLevel();
        }
    }

    IEnumerator PlayStartAnimation()
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");

            yield return new WaitForSeconds(transitionDuration);
        }

        RestorePlayerData();
    }

    void InitializeLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < levelSceneNames.Length; i++)
        {
            if (currentSceneName.Equals(levelSceneNames[i], System.StringComparison.OrdinalIgnoreCase))
            {
                currentLevelIndex = i;
                break;
            }
        }

        if (currentSceneName == "Level3" || !IsLevelScene(currentSceneName))
        {
            timerActive = false;
            if (timerPanel != null)
                timerPanel.SetActive(false);
        }
        else
        {
            timeRemaining = levelDuration;
            timerActive = true;
            if (timerPanel != null)
                timerPanel.SetActive(true);
        }
    }

    bool IsLevelScene(string sceneName)
    {
        foreach (string levelName in levelSceneNames)
        {
            if (sceneName.Equals(levelName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    void UpdateTimer()
    {
        timeRemaining -= Time.deltaTime;

        timeRemaining = Mathf.Max(0f, timeRemaining);

        UpdateTimerDisplay();

        if (timeRemaining <= 10f && timeRemaining > 9.5f)
        {
            if (warningSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(warningSound);
            }
            StartCoroutine(FlashTimer());
        }

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            TransitionToNextLevel();
        }
    }

    void UpdateTimerDisplay()
    {
        string timeString = FormatTime(timeRemaining);

        if (timerText != null)
        {
            timerText.text = timeString;

            if (timeRemaining <= 10f)
                timerText.color = Color.red;
            else if (timeRemaining <= 30f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }
        else if (legacyTimerText != null)
        {
            legacyTimerText.text = timeString;

            if (timeRemaining <= 10f)
                legacyTimerText.color = Color.red;
            else if (timeRemaining <= 30f)
                legacyTimerText.color = Color.yellow;
            else
                legacyTimerText.color = Color.white;
        }
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void TransitionToNextLevel()
    {
        if (isTransitioning) return;

        StartCoroutine(TransitionCoroutine());
    }

    IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;
        timerActive = false;

        SavePlayerData();

        DisablePlayerInput(true);

        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("End");
            yield return new WaitForSeconds(transitionDuration);
        }

        currentLevelIndex++;
        if (currentLevelIndex < levelSceneNames.Length)
        {
            SceneManager.LoadScene(levelSceneNames[currentLevelIndex]);
        }
        else
        {
            Debug.Log("All levels complete!");
            CleanupAndGoToGameOver();
        }
    }

    void SavePlayerData()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            persistentScore = GetCurrentScore();
            persistentHealth = gameManager.playerCurrentHealth;
            persistentMaxHealth = gameManager.playerMaxHealth;

            Debug.Log($"Saved - Score: {persistentScore}, Health: {persistentHealth}/{persistentMaxHealth}");
        }
    }

    void RestorePlayerData()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            SetScore(persistentScore);

            if (persistentHealth > 0)
            {
                gameManager.playerCurrentHealth = persistentHealth;
                gameManager.playerMaxHealth = persistentMaxHealth;
                gameManager.UpdateHealthBar();

                Debug.Log($"Restored - Score: {persistentScore}, Health: {persistentHealth}/{persistentMaxHealth}");
            }
        }
    }

    int GetCurrentScore()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            return gameManager.GetScore();
        }
        return persistentScore;
    }

    void SetScore(int score)
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SetScore(score);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsLevelScene(scene.name))
        {
            StopTimer();

            if (scene.name == "GameOver" || scene.name == "MainMenu" || scene.name == "High-Scores")
            {
                Destroy(gameObject);
            }
            return;
        }

        if (scene.name == "Level3")
        {
            StopTimer();
            isTransitioning = false;
            DisablePlayerInput(false);
            RestorePlayerData();
            return;
        }

        if (scene.name == "Level1" || scene.name == "Level2")
        {
            if (timerPanel != null)
            {
                timerPanel.SetActive(true);
            }
            else
            {
                CreateTimerUI();
            }
        }

        isTransitioning = false;

        GameObject transitionObj = GameObject.Find("SceneTransition");
        if (transitionObj != null)
        {
            transitionAnimator = transitionObj.GetComponent<Animator>();
        }

        DisablePlayerInput(false);
        InitializeLevel();

        StartCoroutine(PlayStartAnimation());
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
        }

        AsteroidSpawner asteroidSpawner = FindObjectOfType<AsteroidSpawner>();
        if (asteroidSpawner != null)
        {
            asteroidSpawner.SetSpawning(!disable);
        }
    }

    IEnumerator FlashTimer()
    {
        for (int i = 0; i < 3; i++)
        {
            if (timerPanel != null)
            {
                timerPanel.transform.localScale = Vector3.one * 1.2f;
                yield return new WaitForSeconds(0.1f);
                timerPanel.transform.localScale = Vector3.one;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void CreateTimerUI()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "Level1" && currentScene != "Level2")
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject existingTimer = GameObject.Find("TimerPanel");
        if (existingTimer != null)
        {
            timerPanel = existingTimer;
            timerText = existingTimer.GetComponentInChildren<TextMeshProUGUI>();
            if (timerText == null)
            {
                legacyTimerText = existingTimer.GetComponentInChildren<Text>();
            }
            return;
        }

        timerPanel = new GameObject("TimerPanel");
        timerPanel.transform.SetParent(canvas.transform);

        RectTransform panelRect = timerPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -50);
        panelRect.sizeDelta = new Vector2(200, 60);

        Image panelImage = timerPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        GameObject textObj = new GameObject("TimerText");
        textObj.transform.SetParent(timerPanel.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        try
        {
            timerText = textObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "00:00";
            timerText.fontSize = 36;
            timerText.alignment = TextAlignmentOptions.Center;
        }
        catch
        {
            legacyTimerText = textObj.AddComponent<Text>();
            legacyTimerText.text = "00:00";
            legacyTimerText.fontSize = 30;
            legacyTimerText.alignment = TextAnchor.MiddleCenter;
            legacyTimerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        DontDestroyOnLoad(timerPanel.transform.root.gameObject);
    }

    public float GetTimeRemaining() => timeRemaining;
    public bool IsTransitioning() => isTransitioning;
    public int GetCurrentLevel() => currentLevelIndex + 1;

    public void ForceNextLevel()
    {
        if (!isTransitioning)
        {
            TransitionToNextLevel();
        }
    }

    public void StopTimer()
    {
        timerActive = false;
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }

    public void CleanupAndGoToGameOver()
    {
        StopTimer();

        if (ScoreManager.Instance != null)
        {
            PlayerPrefs.SetInt("LastScore", persistentScore);
            PlayerPrefs.Save();
        }

        if (timerPanel != null)
        {
            Destroy(timerPanel.transform.root.gameObject);
        }

        SceneManager.LoadScene("GameOver");
    }

    public static void ResetPersistentData()
    {
        persistentScore = 0;
        persistentHealth = -1;
        persistentMaxHealth = 5;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (timerPanel != null)
        {
            Destroy(timerPanel.transform.root.gameObject);
        }
    }
}