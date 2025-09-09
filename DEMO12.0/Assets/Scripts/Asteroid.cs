using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [Header("Asteroid Configuration")]
    public AsteroidSize asteroidSize = AsteroidSize.Medium;
    public float speed = 3f;

    [Header("Damage Settings")]
    public int smallAsteroidDamage = 1;
    public int mediumAsteroidDamage = 2;
    public int largeAsteroidDamage = 3;

    private Vector2 moveDirection;
    private bool isInitialized = false;

    public enum AsteroidSize
    {
        Small,
        Medium,
        Large
    }

    void Start()
    {
        gameObject.tag = "Asteroid";

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            gameObject.AddComponent<PolygonCollider2D>();
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        if (IsOffScreen())
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Vector2 direction, float asteroidSpeed)
    {
        moveDirection = direction.normalized;
        speed = asteroidSpeed;
        isInitialized = true;
    }

    private bool IsOffScreen()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);

        return viewportPos.x < -0.2f || viewportPos.x > 1.2f ||
               viewportPos.y < -0.2f || viewportPos.y > 1.2f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            int damage = GetDamageAmount();

            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.PlayerTakeDamage(damage);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            //nu afecteaza
        }
    }

    private int GetDamageAmount()
    {
        switch (asteroidSize)
        {
            case AsteroidSize.Small:
                return smallAsteroidDamage;
            case AsteroidSize.Medium:
                return mediumAsteroidDamage;
            case AsteroidSize.Large:
                return largeAsteroidDamage;
            default:
                return mediumAsteroidDamage;
        }
    }
    public void SetAsteroidSize(AsteroidSize size)
    {
        asteroidSize = size;

        float scale = 1f;
        switch (size)
        {
            case AsteroidSize.Small:
                scale = 0.7f;
                break;
            case AsteroidSize.Medium:
                scale = 1f;
                break;
            case AsteroidSize.Large:
                scale = 1.3f;
                break;
        }
        transform.localScale = Vector3.one * scale;
    }
}