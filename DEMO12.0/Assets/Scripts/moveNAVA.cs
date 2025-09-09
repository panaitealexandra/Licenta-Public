using UnityEngine;

public class MoveNAVA : MonoBehaviour
{
    public float moveSpeed = 5f;
    public GameObject playerLaserPrefab;

    void Update()
    {
        HandleMovement();
        HandleShooting();
    }

    void HandleMovement()
    {
        if (Input.mousePresent && Input.GetMouseButton(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f;
            transform.position = mousePosition;
        }
        else
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;
            transform.position += move;
        }
    }

    void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
        {
            ShootLaser();
        }
    }

    void ShootLaser()
    {
        if (playerLaserPrefab != null)
        {
            Instantiate(playerLaserPrefab, transform.position, Quaternion.identity);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlayerShoot();
            }
        }
    }
}