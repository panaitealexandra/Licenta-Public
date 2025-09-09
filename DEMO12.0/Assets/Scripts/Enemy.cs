using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float stoppingDistance = 5f;
    public float retreatDistance = 3f;

    [Header("Attack")]
    public float timeBetweenShots = 2f;
    public GameObject laserPrefab;

    [Header("Asteroid Avoidance")]
    public float asteroidDetectionRadius = 3f;
    public float avoidanceForce = 5f;
    public LayerMask asteroidLayer = -1; 

    private Transform target;
    private float nextShotTime;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found by Enemy at Start.");
        }

        nextShotTime = Time.time + timeBetweenShots;

        gameObject.tag = "Enemy";

        if (GetComponent<EnemyHealth>() == null)
        {
            gameObject.AddComponent<EnemyHealth>();
        }
    }

    private void Update()
    {
        if (target == null) return;

        Vector2 baseMovement = CalculateBaseMovement();

        Vector2 avoidance = CalculateAsteroidAvoidance();

        Vector2 finalMovement = baseMovement + avoidance;

        if (finalMovement.magnitude > 0)
        {
            transform.position = Vector2.MoveTowards(transform.position,
                transform.position + (Vector3)finalMovement.normalized,
                speed * Time.deltaTime);
        }

        if (Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + timeBetweenShots;
        }

        if (!IsVisibleToCamera() && Vector2.Distance(transform.position, target.position) > 20f)
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.EnemyDestroyed();
            }
            Destroy(gameObject);
        }
    }

    private Vector2 CalculateBaseMovement()
    {
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        Vector2 directionToPlayer = (target.position - transform.position).normalized;

        if (distanceToTarget > stoppingDistance)
        {
            return directionToPlayer;
        }
        else if (distanceToTarget < retreatDistance)
        {
            return -directionToPlayer;
        }

        return Vector2.zero;
    }

    private Vector2 CalculateAsteroidAvoidance()
    {
        Vector2 avoidanceDirection = Vector2.zero;

        Collider2D[] nearbyAsteroids = Physics2D.OverlapCircleAll(transform.position, asteroidDetectionRadius);

        int asteroidCount = 0;
        foreach (Collider2D col in nearbyAsteroids)
        {
            if (col.CompareTag("Asteroid"))
            {
                Vector2 awayFromAsteroid = (Vector2)(transform.position - col.transform.position);
                float distance = awayFromAsteroid.magnitude;

                if (distance > 0)
                {
                    float strength = 1f - (distance / asteroidDetectionRadius);
                    avoidanceDirection += awayFromAsteroid.normalized * strength;
                    asteroidCount++;
                }
            }
        }

        if (asteroidCount > 0)
        {
            avoidanceDirection = avoidanceDirection.normalized * avoidanceForce;
        }

        return avoidanceDirection;
    }

    private void Shoot()
    {
        if (laserPrefab != null && target != null)
        {
            Instantiate(laserPrefab, transform.position, Quaternion.identity);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayEnemyShoot();
            }
        }
    }

    private bool IsVisibleToCamera()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, asteroidDetectionRadius);
    }
}