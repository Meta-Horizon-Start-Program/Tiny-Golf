using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public List<Transform> startingPositions = new();

    public NetworkObject ballNOPrefab;
    public NetworkObject goldenBallNOPrefab;

    public bool hasGoldenBall = false;
    
    private NetworkObject spawnedballNO;

    private int currentHoleId = 0;
    private int currentPlayerId = 0;

    public static GameManager Instance { get; private set; }
    public Leaderboard leaderboard;

    private float timer = 0;

    private void Awake() => Instance = this;


    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        SpawnBallForCurrentHoleAndPlayer();
    }

    private void Update()
    {
        if(IsServer)
        {
            timer += Time.deltaTime;
        }
    }

    [ClientRpc()]
    public void SubmitScoreClientRpc(long score, ClientRpcParams clientRpcParams = default)
    {
        if(leaderboard)
            leaderboard.SubmitScore(score);
    }

    public void SubmitScore()
    {
        ulong targetClientID = NetworkManager.ConnectedClientsIds[currentPlayerId];

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientID }
            }
        };

        SubmitScoreClientRpc((long) timer, clientRpcParams);
    }

    public void OnBallScoredServer()
    {
        if (!IsServer)
            return;

        SubmitScore();
        timer = 0;

        var connectedPlayers = NetworkManager.ConnectedClientsIds;

        //incrementing the current player
        currentPlayerId++;

        //if all player have played
        if (currentPlayerId >= connectedPlayers.Count)
        {
            //go to next hole
            currentHoleId++;
            currentPlayerId = 0;
        }

        //we have played all the hole
        if (currentHoleId >= startingPositions.Count)
        {
            //go back to the first hole
            currentHoleId = 0;
        }

        SpawnBallForCurrentHoleAndPlayer();
    }

    private void SpawnBallForCurrentHoleAndPlayer()
    {
        if (!IsServer)
            return;

        ulong targetClientId = NetworkManager.ConnectedClientsIds[currentPlayerId];

        if (spawnedballNO != null && spawnedballNO.IsSpawned)
        {
            spawnedballNO.Despawn(true);
            spawnedballNO = null;
        }

        Transform spawnT = startingPositions[currentHoleId];
        NetworkObject newBall = Instantiate(hasGoldenBall ? goldenBallNOPrefab : ballNOPrefab, spawnT.position, spawnT.rotation);

        newBall.SpawnWithOwnership(targetClientId, true);

        spawnedballNO = newBall;
    }

    public void RespawnWithGoldenBall()
    {
        RespawnWithGoldenBallServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RespawnWithGoldenBallServerRpc()
    {
        hasGoldenBall = true;
        SpawnBallForCurrentHoleAndPlayer();
    }
}
