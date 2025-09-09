using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderBoss : MonoBehaviour
{
    [Header("Boss Configuration")]
    public int bodyHealth = 10; 
    public float shootInterval = 3f;
    public GameObject bossBulletPrefab;

    [Header("Circular Wave Attack")]
    public int bulletsPerWave = 8; 
    public float bulletSpeed = 3f;
    public float waveSpreadAngle = 360f; 

    [Header("Movement")]
    public float moveSpeed = 1f;
    public float descendSpeed = 2f;

    [Header("Distance Management")]
    public float optimalDistance = 5f; 
    public float tooCloseDistance = 3f; 
    public float tooFarDistance = 7f; 
    public float distanceMovementSpeed = 1.5f; 
    public bool enableDistanceManagement = true;

    [Header("Leg References")]
    public SpiderLeg[] spiderLegs; 

    [Header("Difficulty Scaling")]
    public float shootIntervalWithAllLegs = 3f;
    public float shootIntervalNoLegs = 1.5f; 

    private GameObject player;
    private GameManager gameManager;
    private bool isDestroyed = false;
    private Vector3 targetPosition;
    private bool hasReachedCenter = false;

    private int legsRemaining;
    private bool bodyVulnerable = false;

    private SpriteRenderer bodyRenderer;
    private Color originalColor;

    private enum BossState { Descending, Fighting, Defeated }
    private BossState currentState = BossState.Descending;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        gameManager = FindObjectOfType<GameManager>();

        bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.color;
        }

        legsRemaining = spiderLegs.Length;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Debug.Log($"SpiderBoss initialized with {legsRemaining} legs!");
    }

    void Update()
    {
        if (isDestroyed) return;

        switch (currentState)
        {
            case BossState.Descending:
                DescendToCenter();
                break;
            case BossState.Fighting:
                if (enableDistanceManagement && player != null)
                {
                    DistanceBasedMovement();
                }
                break;
        }
    }

    public void SetTargetPosition(Vector3 centerPosition)
    {
        targetPosition = centerPosition;
        targetPosition.z = transform.position.z;
    }

    void DescendToCenter()
    {
        if (transform.position.y > targetPosition.y)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                new Vector3(transform.position.x, targetPosition.y, transform.position.z),
                descendSpeed * Time.deltaTime);
        }
        else
        {
            hasReachedCenter = true;
            currentState = BossState.Fighting;
            StartCoroutine(ShootingRoutine());
            Debug.Log("SpiderBoss ready to fight!");
        }
    }

    void DistanceBasedMovement()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;

        Vector2 movement = Vector2.zero;

        if (distanceToPlayer < tooCloseDistance)
        {
            movement = -directionToPlayer * distanceMovementSpeed;
        }
        else if (distanceToPlayer > tooFarDistance)
        {
            movement = directionToPlayer * distanceMovementSpeed;
        }
        else if (Mathf.Abs(distanceToPlayer - optimalDistance) > 0.5f)
        {
            if (distanceToPlayer < optimalDistance)
            {
                movement = -directionToPlayer * distanceMovementSpeed * 0.5f;
            }
            else
            {
                movement = directionToPlayer * distanceMovementSpeed * 0.5f;
            }
        }

        float legSpeedModifier = (0.5f + (0.5f * legsRemaining / spiderLegs.Length));
        movement *= legSpeedModifier;

        transform.position += (Vector3)movement * Time.deltaTime;

        if (Random.Range(0f, 1f) < 0.02f) 
        {
            Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
            transform.position += (Vector3)randomOffset;
        }
    }

    IEnumerator ShootingRoutine()
    {
        while (!isDestroyed && currentState == BossState.Fighting)
        {
            float currentInterval = Mathf.Lerp(shootIntervalNoLegs, shootIntervalWithAllLegs,
                (float)legsRemaining / spiderLegs.Length);

            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
                if (distanceToPlayer < tooCloseDistance)
                {
                    currentInterval *= 0.7f; 
                }
            }

            yield return new WaitForSeconds(currentInterval);

            if (!isDestroyed && player != null)
            {
                FireCircularWave();
            }
        }
    }

    void FireCircularWave()
    {
        if (bossBulletPrefab == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyShoot();
        }

        float angleStep = waveSpreadAngle / bulletsPerWave;
        float startAngle = -waveSpreadAngle / 2f;

        if (legsRemaining == 0)
        {
            angleStep = 360f / bulletsPerWave;
            startAngle = 0f;
        }

        if (player != null && waveSpreadAngle < 360f)
        {
            Vector2 toPlayer = (player.transform.position - transform.position).normalized;
            float angleToPlayer = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            startAngle = angleToPlayer - waveSpreadAngle / 2f;
        }

        for (int i = 0; i < bulletsPerWave; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.down;

            GameObject bullet = Instantiate(bossBulletPrefab, transform.position, Quaternion.identity);

            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * bulletSpeed;
            }

            bullet.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

            if (bullet.GetComponent<BossBullet>() == null)
            {
                bullet.AddComponent<BossBullet>();
            }

            Destroy(bullet, 5f);
        }

        Debug.Log($"Fired circular wave with {bulletsPerWave} bullets!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player_Laser") || other.name.Contains("Laser"))
        {
            if (bodyVulnerable)
            {
                TakeBodyDamage(1);
                Destroy(other.gameObject);
            }

        }
    }

    public void OnLegDestroyed(int legID)
    {
        legsRemaining--;
        Debug.Log($"Leg {legID} destroyed! {legsRemaining} legs remaining.");

        if (legsRemaining <= 0)
        {
            bodyVulnerable = true;
            StartCoroutine(EnragedMode());
        }
    }

    IEnumerator EnragedMode()
    {
        Debug.Log("All legs destroyed! Spider enters ENRAGED mode!");

        if (bodyRenderer != null)
        {
            for (int i = 0; i < 3; i++)
            {
                bodyRenderer.color = Color.red;
                yield return new WaitForSeconds(0.2f);
                bodyRenderer.color = originalColor;
                yield return new WaitForSeconds(0.2f);
            }
        }

        distanceMovementSpeed *= 1.5f;
        moveSpeed *= 1.5f;

    }

    void TakeBodyDamage(int damage)
    {
        if (!bodyVulnerable || isDestroyed) return;

        bodyHealth -= damage;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBossHit();
        }

        StartCoroutine(FlashDamage());

        Debug.Log($"Spider body hit! Health: {bodyHealth}");

        if (bodyHealth <= 0)
        {
            DestroyBoss();
        }
    }

    IEnumerator FlashDamage()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (!isDestroyed)
            {
                bodyRenderer.color = bodyVulnerable ? Color.red : originalColor;
            }
        }
    }

    void DestroyBoss()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        currentState = BossState.Defeated;

        if (gameManager != null)
        {
            gameManager.SpiderBossDestroyed();
        }

        Debug.Log("Spider Boss DEFEATED!");
        StartCoroutine(DeathAnimation());
    }

    IEnumerator DeathAnimation()
    {
        float shakeAmount = 0.5f;
        Vector3 originalPos = transform.position;

        for (int i = 0; i < 20; i++)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;

            if (bodyRenderer != null)
            {
                bodyRenderer.color = i % 2 == 0 ? Color.white : Color.red;
            }

            shakeAmount *= 0.9f;
            yield return new WaitForSeconds(0.1f);
        }

        if (bodyRenderer != null)
        {
            float fadeTime = 1f;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeTime);
                Color c = bodyRenderer.color;
                c.a = alpha;
                bodyRenderer.color = c;
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tooFarDistance);
    }

    public bool IsDestroyed() => isDestroyed;
    public int GetRemainingLegs() => legsRemaining;
    public bool IsBodyVulnerable() => bodyVulnerable;
}