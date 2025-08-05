using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddSpeedOnTriggerEnter : MonoBehaviour
{
    public string targetTag;

    public GameManager gameManager;
    private Collider clubCollider;
    private Vector3 previousPosition;
    private Vector3 velocity;

    // Start is called before the first frame update
    void Start()
    {
        previousPosition = transform.position;
        clubCollider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        velocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(targetTag))
        {
            Debug.Log("Colliding..");

            Vector3 collisionPosition = clubCollider.ClosestPoint(other.transform.position);
            Vector3 collisionNormal = other.transform.position - collisionPosition;

            Vector3 projectedVelocity = Vector3.Project(velocity, collisionNormal);

            Rigidbody rb = other.attachedRigidbody;
            rb.linearVelocity = projectedVelocity;

            gameManager.currentHitNumber++;
        }
    }
}
