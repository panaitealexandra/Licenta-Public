using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BudyAI : MonoBehaviour
{
    public enum BudyMood
    {
        Calm,       
        Aggressive, 
        Scared,
        Supportive  
    }

    [Header("Current State")]
    public BudyMood currentMood = BudyMood.Calm;

    [Header("References")]
    public Transform player;
    public GameObject bulletPrefab;
    public Transform shootPoint;

    [Header("Movement Settings")]
    public float normalFollowDistance = 2f;
    public float scaredFollowDistance = 4f;
    public float aggressiveFollowDistance = 1.5f;
    public float moveSpeed = 4f;
    public float healingDistance = 0.5f;

    [Header("Combat Settings")]
    public float calmShootInterval = 5f;      
    public float aggressiveShootInterval = 1f; 
    public float scaredShootInterval = 3f;    
    public float enemyDetectionRadius = 8f;
    public float bulletSpeed = 10f;

    [Header("Health Monitoring")]
    public float lowHealthThreshold = 0.3f;    // 30% health = scared/supportive
    public float criticalHealthThreshold = 0.2f; // 20% health = supportive
    public float goodHealthThreshold = 0.7f;    // 70% health = can be aggressive

    [Header("Enemy Awareness")]
    public int manyEnemiesThreshold = 3;       // 3+ enemies = aggressive

    [Header("Healing")]
    public int healAmount = 1;
    public float healCooldown = 10f;

    [Header("Visual/Audio Feedback")]
    public Color calmColor = Color.green;
    public Color aggressiveColor = Color.red;
    public Color scaredColor = Color.yellow;
    public Color supportiveColor = Color.cyan;

    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private float nextShootTime;
    private float nextHealTime;
    private float moodCheckInterval = 0.5f;
    private float nextMoodCheck;
    private List<GameObject> nearbyEnemies = new List<GameObject>();
    private bool isHealing = false;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        gameManager = FindObjectOfType<GameManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisualForMood();

        StartCoroutine(MoodUpdateRoutine());
    }

    void Update()
    {
        if (player == null) return;

        DetectNearbyEnemies();
        ExecuteMoodBehavior();
    }

    IEnumerator MoodUpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(moodCheckInterval);
            UpdateMood();
        }
    }

    void UpdateMood()
    {
        if (gameManager == null || player == null) return;

        float healthPercentage = (float)gameManager.playerCurrentHealth / gameManager.playerMaxHealth;
        int enemyCount = nearbyEnemies.Count;

        BudyMood previousMood = currentMood;

        // update mood based on game context
        if (healthPercentage <= criticalHealthThreshold)
        {
            currentMood = BudyMood.Supportive;
        }
        else if (healthPercentage <= lowHealthThreshold)
        {
            currentMood = BudyMood.Scared;
        }
        else if (enemyCount >= manyEnemiesThreshold && healthPercentage >= goodHealthThreshold)
        {
            currentMood = BudyMood.Aggressive;
        }
        else
        {
            currentMood = BudyMood.Calm;
        }

        if (previousMood != currentMood)
        {
            OnMoodChanged(previousMood);
        }
    }

    void OnMoodChanged(BudyMood previousMood)
    {
        UpdateVisualForMood();
    }

    void ExecuteMoodBehavior()
    {
        switch (currentMood)
        {
            case BudyMood.Calm:
                CalmBehavior();
                break;
            case BudyMood.Aggressive:
                AggressiveBehavior();
                break;
            case BudyMood.Scared:
                ScaredBehavior();
                break;
            case BudyMood.Supportive:
                SupportiveBehavior();
                break;
        }
    }

    void CalmBehavior()
    {
        FollowPlayer(normalFollowDistance);

        if (Time.time >= nextShootTime && nearbyEnemies.Count > 0)
        {
            ShootAtNearestEnemy();
            nextShootTime = Time.time + calmShootInterval;
        }
    }

    void AggressiveBehavior()
    {
        FollowPlayer(aggressiveFollowDistance);

        if (Time.time >= nextShootTime && nearbyEnemies.Count > 0)
        {
            ShootAtNearestEnemy();
            nextShootTime = Time.time + aggressiveShootInterval;
        }
    }

    void ScaredBehavior()
    {
        FollowPlayer(scaredFollowDistance);

        if (Random.Range(0f, 1f) < 0.02f)
        {
            Vector2 dodgeDirection = Random.insideUnitCircle * 2f;
            transform.position += (Vector3)dodgeDirection * Time.deltaTime;
        }

        if (Time.time >= nextShootTime && nearbyEnemies.Count > 0)
        {
            ShootAtNearestEnemy();
            nextShootTime = Time.time + scaredShootInterval;
        }
    }

    void SupportiveBehavior()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > healingDistance)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * moveSpeed * 1.5f * Time.deltaTime);
        }
        else if (!isHealing && Time.time >= nextHealTime)
        {
            StartCoroutine(HealPlayer());
        }
    }

    void FollowPlayer(float desiredDistance)
    {
        if (player == null) return;

        float currentDistance = Vector2.Distance(transform.position, player.position);

        if (Mathf.Abs(currentDistance - desiredDistance) > 0.5f)
        {
            Vector2 direction;

            if (currentDistance > desiredDistance)
            {
                direction = (player.position - transform.position).normalized;
            }
            else
            {
                direction = (transform.position - player.position).normalized;
            }

            transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
        }
    }

    void DetectNearbyEnemies()
    {
        nearbyEnemies.Clear();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, enemyDetectionRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                nearbyEnemies.Add(col.gameObject);
            }
        }
    }

    void ShootAtNearestEnemy()
    {
        if (nearbyEnemies.Count == 0 || bulletPrefab == null) return;

        GameObject nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject enemy in nearbyEnemies)
        {
            if (enemy != null)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        if (nearestEnemy != null)
        {
            Vector3 shootPos = shootPoint != null ? shootPoint.position : transform.position;
            GameObject bullet = Instantiate(bulletPrefab, shootPos, Quaternion.identity);

            Vector2 direction = (nearestEnemy.transform.position - transform.position).normalized;

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * bulletSpeed;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    IEnumerator HealPlayer()
    {
        isHealing = true;

        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = supportiveColor;
        }

        if (gameManager != null)
        {
            gameManager.HealPlayer(healAmount);
        }

        nextHealTime = Time.time + healCooldown;
        isHealing = false;

        yield return new WaitForSeconds(0.5f);
        UpdateVisualForMood();
    }

    void UpdateVisualForMood()
    {
        if (spriteRenderer == null) return;

        switch (currentMood)
        {
            case BudyMood.Calm:
                spriteRenderer.color = calmColor;
                break;
            case BudyMood.Aggressive:
                spriteRenderer.color = aggressiveColor;
                break;
            case BudyMood.Scared:
                spriteRenderer.color = scaredColor;
                break;
            case BudyMood.Supportive:
                spriteRenderer.color = supportiveColor;
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyDetectionRadius);

        if (player != null)
        {
            Gizmos.color = calmColor;
            Gizmos.DrawWireSphere(player.position, normalFollowDistance);

            Gizmos.color = aggressiveColor;
            Gizmos.DrawWireSphere(player.position, aggressiveFollowDistance);

            Gizmos.color = scaredColor;
            Gizmos.DrawWireSphere(player.position, scaredFollowDistance);
        }
    }

    public BudyMood GetCurrentMood() => currentMood;

    public void ForceChangeMood(BudyMood newMood)
    {
        BudyMood oldMood = currentMood;
        currentMood = newMood;
        OnMoodChanged(oldMood);
    }

    //update mood based on chat mess
    public void UpdateMoodFromChat(string message)
    {
        message = message.ToLower();

        if (message.Contains("attack") || message.Contains("fight") ||
            message.Contains("destroy") || message.Contains("kill") ||
            message.Contains("shoot") || message.Contains("get them") ||
            message.Contains("blast") || message.Contains("fire"))
        {
            ForceChangeMood(BudyMood.Aggressive);
        }
        else if (message.Contains("scared") || message.Contains("fear") ||
                 message.Contains("help") || message.Contains("run") ||
                 message.Contains("hide") || message.Contains("danger") ||
                 message.Contains("terrified") || message.Contains("afraid"))
        {
            ForceChangeMood(BudyMood.Scared);
        }
        else if (message.Contains("heal") || message.Contains("support") ||
                 message.Contains("help me") || message.Contains("dying") ||
                 message.Contains("low health") || message.Contains("need health"))
        {
            ForceChangeMood(BudyMood.Supportive);
        }
        else if (message.Contains("calm") || message.Contains("relax") ||
                 message.Contains("good job") || message.Contains("amazing") ||
                 message.Contains("great") || message.Contains("peaceful") ||
                 message.Contains("nice") || message.Contains("well done") ||
                 message.Contains("you are doing amazing") || message.Contains("it's ok"))
        {
            ForceChangeMood(BudyMood.Calm);
        }
    }
}