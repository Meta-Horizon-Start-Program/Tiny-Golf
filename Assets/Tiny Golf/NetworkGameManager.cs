using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    
}



/*
 * public List<Transform> startingPositions = new();

    public NetworkObject ballNOPrefab;
    public NetworkObject spawnedballNO;

    private int currentHoleId = 0;
    private int currentPlayerId = 0;

    public static NetworkGameManager Instance { get; private set; }

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) 
            return;

        SpawnBallForCurrentHoleAndPlayer();
    }

    public void OnBallScoredServer()
    {
        if (!IsServer)
            return;

        var connectedPlayers = NetworkManager.ConnectedClientsIds;

        currentPlayerId++;

        if (currentPlayerId >= connectedPlayers.Count)
        {
            currentHoleId++;
            currentPlayerId = 0;
        }

        if (currentHoleId >= startingPositions.Count)
        {
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
        NetworkObject newBall = Instantiate(ballNOPrefab, spawnT.position, spawnT.rotation);

        newBall.SpawnWithOwnership(targetClientId, true);

        spawnedballNO = newBall;
    }
 */