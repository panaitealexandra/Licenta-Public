using UnityEngine;
using static UnityEngine.GraphicsBuffer;


using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class BudyFollows : MonoBehaviour
{
    [Header("Following Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(1f, 1f, 0f);
    public float followSpeed = 5f;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void Update()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;

            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
}

