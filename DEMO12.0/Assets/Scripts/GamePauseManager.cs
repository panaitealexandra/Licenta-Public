using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GamePauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;
    public Text pauseText;

    [Header("Pause Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool allowPauseInChat = false;

    private bool isPaused = false;
    private bool chatIsOpen = false;
    private List<string> pauseReasons = new List<string>();

    // singleton pattern for easy access
    public static GamePauseManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        UpdatePauseState();
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey) && !chatIsOpen)
        {
            ToggleManualPause();
        }
    }

    public void PauseGame(string reason)
    {
        if (!pauseReasons.Contains(reason))
        {
            pauseReasons.Add(reason);
        }

        UpdatePauseState();
    }

    public void ResumeGame(string reason)
    {
        pauseReasons.Remove(reason);
        UpdatePauseState();
    }

    public void ToggleManualPause()
    {
        if (pauseReasons.Contains("manual"))
        {
            ResumeGame("manual");
        }
        else
        {
            PauseGame("manual");
        }
    }

    public void SetChatOpen(bool isOpen)
    {
        chatIsOpen = isOpen;

        if (isOpen)
        {
            PauseGame("chat");
        }
        else
        {
            ResumeGame("chat");
        }
    }

    void UpdatePauseState()
    {
        bool shouldBePaused = pauseReasons.Count > 0;

        if (shouldBePaused != isPaused)
        {
            isPaused = shouldBePaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (pausePanel != null)
            {
                bool showPanel = pauseReasons.Contains("manual");
                pausePanel.SetActive(showPanel);
            }

            if (pauseText != null)
            {
                if (pauseReasons.Contains("chat"))
                {
                    pauseText.text = "Chatting with Budy...";
                }
                else if (pauseReasons.Contains("manual"))
                {
                    pauseText.text = "Game Paused\nPress ESC to Resume";
                }
                else
                {
                    pauseText.text = "Game Paused";
                }
            }

            TogglePlayerInput(!isPaused);
        }
    }

    void TogglePlayerInput(bool enable)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            MoveNAVA playerMovement = player.GetComponent<MoveNAVA>();
            if (playerMovement != null)
            {
                playerMovement.enabled = enable;
            }
        }

        AsteroidSpawner asteroidSpawner = FindObjectOfType<AsteroidSpawner>();
        if (asteroidSpawner != null)
        {
            asteroidSpawner.SetSpawning(enable);
        }

        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            enemy.enabled = enable;
        }
    }

    public bool IsPaused() => isPaused;
    public bool IsChatOpen() => chatIsOpen;
    public bool IsManuallyPaused() => pauseReasons.Contains("manual");
}