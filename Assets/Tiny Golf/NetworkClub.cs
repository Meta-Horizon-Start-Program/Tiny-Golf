using UnityEngine;
using Unity.Netcode;

public class NetworkClub : NetworkBehaviour
{
    [Header("Detect")]
    public string targetTag = "ball";
    [SerializeField] private Collider clubCollider;

    private Rigidbody ballRb;        // Ball rigidbody (same object for everyone)
    private NetworkObject ballNetObj;


    private Vector3 prevPos;
    private Vector3 velocity;

    private void Start()
    {
        if (!clubCollider) clubCollider = GetComponent<Collider>();
        prevPos = transform.position;
    }

    private void Update()
    {
        velocity = (transform.position - prevPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        prevPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the local player's club runs this code
        if (!IsOwner) 
            return;

        if (!other.CompareTag(targetTag)) 
            return;

        ballRb = other.attachedRigidbody;
        ballNetObj = other.GetComponent<NetworkObject>();

        // Only the ball owner is allowed to "hit" it
        if (ballNetObj.OwnerClientId != NetworkManager.LocalClientId) 
            return;

        // Compute projected velocity (same as before)
        Vector3 collisionPos = clubCollider.ClosestPoint(other.transform.position);
        Vector3 collisionNormal = (other.transform.position - collisionPos);
        Vector3 projectedVelocity = Vector3.Project(velocity, collisionNormal);

        // Apply movement LOCALLY (owner-authoritative ball)
        ballRb.linearVelocity = projectedVelocity;
        ballRb.angularVelocity = Vector3.zero;
    }
}
