using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class About : MonoBehaviour
{
    [Header("UI References")]
    public Button exitButton;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(GoToMainMenu);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        ScoreManager.Instance.ResetCurrentScore();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
