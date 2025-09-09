using UnityEngine;

public class Enemy_Laser : MonoBehaviour
{
    public float speed = 6f;
    private Transform player;
    private Vector2 targetPos;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (player != null)
        {
            // Target the player's position at the time of firing
            targetPos = new Vector2(player.position.x, player.position.y);
        }

        // Ensure this object has the correct tag
        gameObject.tag = "Enemy_Laser";
    }

    private void Update()
    {
        // Move towards the targeted position
        transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Destroy when reaching the target position
        if ((Vector2)transform.position == targetPos)
        {
            DestroyLaser();
        }

        // Also destroy if it goes off-screen
        if (!IsVisibleToCamera())
        {
            DestroyLaser();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // The player health component will handle the damage
            DestroyLaser();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DestroyLaser();
    }

    private void DestroyLaser()
    {
        Destroy(gameObject);
    }

    private bool IsVisibleToCamera()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Bounds bounds = GetComponent<Renderer>().bounds;

        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }
}