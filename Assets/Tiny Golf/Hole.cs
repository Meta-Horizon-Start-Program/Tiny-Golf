using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Hole : NetworkBehaviour
{
    [SerializeField] private string targetTag = "ball";
    [SerializeField] private float waitSeconds = 3f;
    bool waiting;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (!other.CompareTag(targetTag))
            return;

        if (!waiting)
            StartCoroutine(ScoreAfterWait());
    }

    private IEnumerator ScoreAfterWait()
    {
        waiting = true;
        yield return new WaitForSeconds(waitSeconds);
        waiting = false;

        GameManager.Instance.OnBallScoredServer();
    }
}
