using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Course")]
    [SerializeField] private List<Transform> startingPositions = new();

    [Header("Ball")]
    [SerializeField] private NetworkObject ballNetObj;
    [SerializeField] private Rigidbody ballRb;

    private int currentHoleId = 0;
    private int currentPlayerId = 0;

    public static NetworkGameManager Instance { get; private set; }

    private void Awake() => Instance = this;


    public override void OnNetworkSpawn()
    {
        if (!IsServer) 
            return;

        GiveBallToCurrentPlayerId();
        ResetBallToCurrentHoleId();
    }


    public void OnBallScoredServer()
    {
        if (!IsServer)
            return;

        var connectedPlayers = NetworkManager.ConnectedClientsIds;

        //go to next player
        currentPlayerId++;

        //if all player have played go to next hole and reset player
        if (currentPlayerId >= connectedPlayers.Count)
        {
            currentHoleId++;
            currentPlayerId = 0;
        }

        //if all hole have been played reset to hole 0 and player 0;
        if (currentHoleId >= startingPositions.Count)
        {
            currentHoleId = 0;
        }

        GiveBallToCurrentPlayerId();
        ResetBallToCurrentHoleId();
    }


    private void GiveBallToCurrentPlayerId()
    {
        ulong target = NetworkManager.ConnectedClientsIds[currentPlayerId];

        if (ballNetObj.OwnerClientId != target)
        {
            ballNetObj.ChangeOwnership(target);
        }
    }

    private void ResetBallToCurrentHoleId()
    {
        var spawn = startingPositions[currentHoleId];

        // Tell the (new) owner to snap the ball locally (no server writes)
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { ballNetObj.OwnerClientId } }
        };
        ResetBallClientRpc(spawn.position, spawn.rotation, target);
    }

    [ClientRpc]
    private void ResetBallClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams rpcParams = default)
    {
        // Only meaningful on the targeted owner when using owner-authoritative ball
        ballRb.position = pos;
        ballRb.rotation = rot;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }
}
