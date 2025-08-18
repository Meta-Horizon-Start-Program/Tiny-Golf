using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
    [SerializeField] private string targetTag = "ball";
    [SerializeField] private float waitSeconds = 3f;
    bool waiting;

    private void OnTriggerEnter(Collider other)
    {
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

        GameManager.Instance.GoToNextHole();
    }
}
