using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Level Configuration")]
    public bool isLevel3 = false;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public int maxEnemies = 3;
    public float spawnInterval = 3f;
    public float spawnDistance = 12f;

    [Header("Spider Boss (Level 3)")]
    public GameObject spiderBossPrefab;
    private GameObject spiderBossInstance;
    private bool bossDefeated = false;

    [Header("Player Health")]
    public int playerMaxHealth = 5;
    public int playerCurrentHealth;
    public Slider healthBar;

    [Header("UI References")]
    public UnityEngine.UI.Text scoreText;
    public TMPro.TextMeshProUGUI tmpScoreText;
    public GameObject gameOverPanel;

    [Header("Game Over Settings")]
    public string gameOverSceneName = "GameOver";
    public float gameOverDelay = 2f;

    private int currentEnemyCount = 0;
    private int score = 0;
    private bool isGameOver = false;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        playerCurrentHealth = playerMaxHealth;

        UpdateHealthBar();
        UpdateScoreText();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        CheckIfLevel3();

        if (!isLevel3)
        {
            StartCoroutine(SpawnEnemies());
        }
        else
        {
            SpawnSpiderBoss();
        }
    }

    private void CheckIfLevel3()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentSceneName.ToLower().Contains("level3") || currentSceneName.ToLower().Contains("boss"))
        {
            isLevel3 = true;
        }
    }

    private void SpawnSpiderBoss()
    {
        if (spiderBossPrefab != null && !bossDefeated)
        {
            Vector2 spawnPos = GetSpiderBossSpawnPosition();
            spiderBossInstance = Instantiate(spiderBossPrefab, spawnPos, Quaternion.identity);

            SpiderBoss spiderBossScript = spiderBossInstance.GetComponent<SpiderBoss>();
            if (spiderBossScript != null)
            {
                Vector3 centerPosition = mainCamera.transform.position;
                centerPosition.z = 0;
                spiderBossScript.SetTargetPosition(centerPosition);
            }

            Debug.Log("SpiderBoss has appeared!");
        }
    }

    private Vector2 GetSpiderBossSpawnPosition()
    {
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;
        Vector2 spawnPos = new Vector2(0, height / 2 + 3);
        return mainCamera.transform.position + new Vector3(spawnPos.x, spawnPos.y, 0);
    }

    IEnumerator SpawnEnemies()
    {
        yield return new WaitForSeconds(2f);

        while (!isGameOver && !isLevel3)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (LevelManager.Instance != null && LevelManager.Instance.IsTransitioning())
            {
                continue;
            }

            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            int randomHealth = Random.Range(1, 6);
            enemyHealth.SetHealth(randomHealth);

            SpriteRenderer renderer = newEnemy.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                float healthRatio = (float)randomHealth / 5f;
                renderer.color = Color.Lerp(Color.green, Color.red, healthRatio);
            }
        }

        currentEnemyCount++;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        int side = Random.Range(0, 4);
        Vector2 spawnPos = Vector2.zero;

        switch (side)
        {
            case 0: // top
                spawnPos = new Vector2(Random.Range(-width / 2, width / 2), height / 2 + 1);
                break;
            case 1: // right
                spawnPos = new Vector2(width / 2 + 1, Random.Range(-height / 2, height / 2));
                break;
            case 2: // bottom
                spawnPos = new Vector2(Random.Range(-width / 2, width / 2), -height / 2 - 1);
                break;
            case 3: // left
                spawnPos = new Vector2(-width / 2 - 1, Random.Range(-height / 2, height / 2));
                break;
        }

        return mainCamera.transform.position + new Vector3(spawnPos.x, spawnPos.y, 0);
    }

    public void EnemyDestroyed()
    {
        currentEnemyCount--;
        score += 10;
        UpdateScoreText();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(score);
        }
    }

    public void SpiderBossDestroyed()
    {
        score += 100;
        UpdateScoreText();
        bossDefeated = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(score);
        }

        Debug.Log("SpiderBoss defeated! Level complete!");
        StartCoroutine(VictorySequence());
    }

    IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(2f);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(score);
            PlayerPrefs.SetInt("LastScore", score);
            PlayerPrefs.Save();
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.StopTimer();
        }

        SceneManager.LoadScene("GameOver");
    }

    public void PlayerTakeDamage(int damage)
    {
        if (isGameOver) return;

        playerCurrentHealth -= damage;
        UpdateHealthBar();

        if (playerCurrentHealth <= 0)
        {
            GameOver();
        }
    }

    public void HealPlayer(int amount)
    {
        if (isGameOver) return;

        playerCurrentHealth = Mathf.Min(playerCurrentHealth + amount, playerMaxHealth);
        UpdateHealthBar();

        Debug.Log("Health restored: +" + amount);
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)playerCurrentHealth / playerMaxHealth;
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
        else if (tmpScoreText != null)
        {
            tmpScoreText.text = "Score: " + score;
        }
    }

    public int GetScore()
    {
        return score;
    }

    public void SetScore(int newScore)
    {
        score = newScore;
        UpdateScoreText();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(score);
        }
    }

    private void GameOver()
    {
        isGameOver = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(score);
            PlayerPrefs.SetInt("LastScore", score);
            PlayerPrefs.Save();
        }

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        Debug.Log("Game Over! Final score: " + score);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            MoveNAVA playerMovement = player.GetComponent<MoveNAVA>();
            if (playerMovement != null)
                playerMovement.enabled = false;

            yield return new WaitForSeconds(1f);

            player.SetActive(false);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        if (spiderBossInstance != null)
        {
            Destroy(spiderBossInstance);
        }

        GameObject buddy = GameObject.Find("Budy");
        if (buddy != null)
        {
            yield return new WaitForSeconds(2f);
            buddy.SetActive(false);
        }

        LevelManager.ResetPersistentData();


        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
        else if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        LevelManager.ResetPersistentData();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetCurrentScore();
        }

        if (LevelManager.Instance != null)
        {
            Destroy(LevelManager.Instance.gameObject);
        }

        SceneManager.LoadScene("Level1");
    }
}