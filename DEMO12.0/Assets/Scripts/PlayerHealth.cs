using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int damageAmount = 1;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private float invincibilityDuration = 1f;
    private bool isInvincible = false;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isInvincible) return;

        if (other.CompareTag("Enemy_Laser"))
        {
            TakeDamage();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInvincible) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage();
        }
    }

    private void TakeDamage()
    {
        if (gameManager != null)
        {
            gameManager.PlayerTakeDamage(damageAmount);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlayerHit();
            }

            StartCoroutine(InvincibilityFrames());
        }
    }

    private System.Collections.IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        // flash effect
        if (spriteRenderer != null)
        {
            for (int i = 0; i < 5; i++)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSeconds(invincibilityDuration / 10);
                spriteRenderer.enabled = true;
                yield return new WaitForSeconds(invincibilityDuration / 10);
            }
        }
        else
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }

        isInvincible = false;
    }
}