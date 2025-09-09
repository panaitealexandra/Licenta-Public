using UnityEngine;

public class SpiderBodyCollider : MonoBehaviour
{
    private Collider2D bodyCollider;
    private SpiderBoss spiderBoss;

    void Start()
    {
        bodyCollider = GetComponent<Collider2D>();
        spiderBoss = GetComponentInParent<SpiderBoss>();
        bodyCollider.enabled = false;
    }

    void Update()
    {
        if (spiderBoss != null && bodyCollider != null)
        {
            bodyCollider.enabled = spiderBoss.IsBodyVulnerable();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player_Laser") || other.name.Contains("Laser"))
        {
            spiderBoss.SendMessage("OnTriggerEnter2D", other);
        }
    }
}