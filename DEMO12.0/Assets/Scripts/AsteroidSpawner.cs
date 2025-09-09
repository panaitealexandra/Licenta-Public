using System.Collections;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Prefabs")]
    public GameObject[] asteroidPrefabs; // diff asteroid sprites

    [Header("Spawning Configuration")]
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 5f;
    public bool spawnAsteroids = true;

    [Header("Speed Configuration")]
    public float minAsteroidSpeed = 2f;
    public float maxAsteroidSpeed = 6f;

    [Header("Size Distribution")]
    [Range(0f, 1f)] public float smallAsteroidChance = 0.5f;
    [Range(0f, 1f)] public float mediumAsteroidChance = 0.35f;
    [Range(0f, 1f)] public float largeAsteroidChance = 0.15f;

    private Camera mainCamera;
    private GameManager gameManager;

    void Start()
    {
        mainCamera = Camera.main;
        gameManager = FindObjectOfType<GameManager>();

        StartCoroutine(SpawnAsteroidsRoutine());
    }

    IEnumerator SpawnAsteroidsRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            if (spawnAsteroids && (gameManager == null || !gameManager.IsGameOver()))
            {
                SpawnAsteroid();
            }
        }
    }

    void SpawnAsteroid()
    {
        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogWarning("no asteroid prefabs assigned!");
            return;
        }

        GameObject asteroidPrefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];

        Vector2 spawnPosition;
        Vector2 moveDirection;

        int spawnSide = Random.Range(0, 3);

        switch (spawnSide)
        {
            case 0: // from top
                spawnPosition = GetTopSpawnPosition();
                moveDirection = Vector2.down;
                break;
            case 1: // from right
                spawnPosition = GetRightSpawnPosition();
                moveDirection = Vector2.left;
                break;
            case 2: // from left
                spawnPosition = GetLeftSpawnPosition();
                moveDirection = Vector2.right;
                break;
            default:
                spawnPosition = GetTopSpawnPosition();
                moveDirection = Vector2.down;
                break;
        }

        GameObject asteroid = Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);

        Asteroid asteroidScript = asteroid.GetComponent<Asteroid>();
        if (asteroidScript == null)
        {
            asteroidScript = asteroid.AddComponent<Asteroid>();
        }

        Asteroid.AsteroidSize size = GetRandomAsteroidSize();
        asteroidScript.SetAsteroidSize(size);

        float speed = Random.Range(minAsteroidSpeed, maxAsteroidSpeed);

        float angleVariation = Random.Range(-15f, 15f);
        moveDirection = Quaternion.Euler(0, 0, angleVariation) * moveDirection;

        asteroidScript.Initialize(moveDirection, speed);

        asteroid.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        RotateAsteroid rotator = asteroid.AddComponent<RotateAsteroid>();
        rotator.rotationSpeed = Random.Range(-50f, 50f);
    }

    Vector2 GetTopSpawnPosition()
    {
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        float x = Random.Range(-width / 2, width / 2);
        float y = height / 2 + 2f;

        return mainCamera.transform.position + new Vector3(x, y, 0);
    }

    Vector2 GetRightSpawnPosition()
    {
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        float x = width / 2 + 2f; 
        float y = Random.Range(-height / 2, height / 2);

        return mainCamera.transform.position + new Vector3(x, y, 0);
    }

    Vector2 GetLeftSpawnPosition()
    {
        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        float x = -width / 2 - 2f;
        float y = Random.Range(-height / 2, height / 2);

        return mainCamera.transform.position + new Vector3(x, y, 0);
    }

    Asteroid.AsteroidSize GetRandomAsteroidSize()
    {
        float random = Random.Range(0f, 1f);

        if (random < smallAsteroidChance)
            return Asteroid.AsteroidSize.Small;
        else if (random < smallAsteroidChance + mediumAsteroidChance)
            return Asteroid.AsteroidSize.Medium;
        else
            return Asteroid.AsteroidSize.Large;
    }

    public void SetSpawning(bool enable)
    {
        spawnAsteroids = enable;
    }
}
public class RotateAsteroid : MonoBehaviour
{
    public float rotationSpeed = 30f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}