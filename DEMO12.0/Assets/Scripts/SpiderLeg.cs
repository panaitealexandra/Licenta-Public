using UnityEngine;
using System.Collections;

public class SpiderLeg : MonoBehaviour
{
    [Header("Leg Configuration")]
    public int legID = 0; 
    public int health = 10; 
    public Transform weakPoint; 
    public float weakPointRadius = 0.5f; 

    [Header("Visual Effects")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;

    private SpiderBoss spiderBoss;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDestroyed = false;
    private Collider2D weakPointCollider;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        spiderBoss = GetComponentInParent<SpiderBoss>();

        if (weakPoint == null)
        {
            GameObject wp = new GameObject("WeakPoint");
            wp.transform.SetParent(transform);
            wp.transform.localPosition = new Vector3(0, 0.5f, 0); 
            weakPoint = wp.transform;
        }

        CreateWeakPointCollider();
    }

    void CreateWeakPointCollider()
    {
        GameObject weakPointObj = weakPoint.gameObject;
        weakPointCollider = weakPointObj.AddComponent<CircleCollider2D>();
        weakPointCollider.isTrigger = true;
        ((CircleCollider2D)weakPointCollider).radius = weakPointRadius;

        WeakPointHandler handler = weakPointObj.AddComponent<WeakPointHandler>();
        handler.parentLeg = this;
    }

    public void OnWeakPointHit(GameObject projectile)
    {
        if (isDestroyed) return;

        TakeHit();

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    public bool CheckHit(Vector2 hitPosition)
    {
        if (isDestroyed) return false;

        float distance = Vector2.Distance(hitPosition, weakPoint.position);

        if (distance <= weakPointRadius)
        {
            TakeHit();
            return true;
        }

        return false;
    }

    void TakeHit()
    {
        health--;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBossHit();
        }

        StartCoroutine(FlashDamage());

        if (health <= 0)
        {
            DestroyLeg();
        }
    }

    IEnumerator FlashDamage()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            if (!isDestroyed)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    void DestroyLeg()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        if (weakPointCollider != null)
        {
            weakPointCollider.enabled = false;
        }

        if (spiderBoss != null)
        {
            spiderBoss.OnLegDestroyed(legID);
        }

        StartCoroutine(FallOff());
    }

    IEnumerator FallOff()
    {
        transform.SetParent(null);

        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.angularVelocity = Random.Range(-180f, 180f); 

        float fadeTime = 2f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeTime);

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (weakPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(weakPoint.position, weakPointRadius);
        }
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }
}

public class WeakPointHandler : MonoBehaviour
{
    public SpiderLeg parentLeg;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player_Laser") || other.name.Contains("Laser"))
        {
            if (parentLeg != null)
            {
                parentLeg.OnWeakPointHit(other.gameObject);
            }
        }
    }
}