using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TMP_InputField nameInputField;
    public Button submitButton;
    public Button mainMenuButton;
    public Button exitButton;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public float fadeInDuration = 1f;

    private bool scoreSubmitted = false;
    private int finalScore = 0;


    void Start()
    {


        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitScore);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (nameInputField != null)
        {
            nameInputField.onSubmit.AddListener(delegate { SubmitScore(); });
            nameInputField.characterLimit = 20;
        }

        ShowGameOver();
    }

    public void ShowGameOver()
    {
        finalScore = ScoreManager.Instance.GetLastScore();

        if (scoreText != null)
            scoreText.text = $"{finalScore}";


        if (nameInputField != null && !scoreSubmitted)
        {
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    void SubmitScore()
    {
        if (scoreSubmitted) return;

        string playerName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Anonymous";
        }

        ScoreManager.Instance.SaveScore(playerName);
        scoreSubmitted = true;

        if (nameInputField != null)
            nameInputField.interactable = false;

        if (submitButton != null)
        {
            submitButton.interactable = false;
            TextMeshProUGUI buttonText = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Score Saved!";
        }

        StartCoroutine(ScoreSavedEffect());
    }

    IEnumerator ScoreSavedEffect()
    {
        if (nameInputField != null)
        {
            Image inputImage = nameInputField.GetComponent<Image>();
            if (inputImage != null)
            {
                Color originalColor = inputImage.color;
                inputImage.color = Color.green;
                yield return new WaitForSeconds(0.5f);
                inputImage.color = originalColor;
            }
        }
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        ScoreManager.Instance.ResetCurrentScore();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(SubmitScore);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }
}
