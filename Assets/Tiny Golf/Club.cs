using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Club : NetworkBehaviour
{
    public string targetTag;

    public float speedMultiplier = 1.3f;

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

    private void OnTriggerStay(Collider other)
    {
        if (!IsOwner)
            return;

        if (!other.CompareTag(targetTag))
            return;

        Vector3 collisionPosition = clubCollider.ClosestPoint(other.transform.position);
        Vector3 collisionNormal = other.transform.position - collisionPosition;

        Vector3 projectedVelocity = Vector3.Project(velocity, collisionNormal);

        Rigidbody rb = other.attachedRigidbody;
        rb.linearVelocity = projectedVelocity * speedMultiplier;
    }
}
