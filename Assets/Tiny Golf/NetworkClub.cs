using UnityEngine;
using Unity.Netcode;

public class NetworkClub : NetworkBehaviour
{

}



/*
   Header("Detect")]
    public string targetTag = "ball";
    [SerializeField] private Collider clubCollider;

    public float speedMultiplier = 1.3f;

    private Vector3 prevPos;
    private Vector3 velocity;

    private void Start()
    {
        if (!clubCollider) 
            clubCollider = GetComponent<Collider>();
        
        prevPos = transform.position;
    }

    private void Update()
    {
        velocity = (transform.position - prevPos) / Time.deltaTime;
        prevPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        if (!other.CompareTag(targetTag))
            return;

        NetworkObject ballNetObj = other.GetComponent<NetworkObject>();
        if (!ballNetObj.IsOwner) 
            return;

        Vector3 collisionPosition = clubCollider.ClosestPoint(other.transform.position);
        Vector3 collisionNormal = other.transform.position - collisionPosition;
        Vector3 projectedVelocity = Vector3.Project(velocity, collisionNormal);

        Rigidbody rb = other.attachedRigidbody;

        rb.linearVelocity = projectedVelocity * speedMultiplier;
    }

*/