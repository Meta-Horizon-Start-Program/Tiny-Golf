using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
    public GameManager gameManager;
    public string targetTag = "ball";

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(targetTag))
        {
            Invoke("GoToNextHoleWait",3);
        }
    }

    public void GoToNextHoleWait()
    {
        gameManager.GoToNextHole();
    }
}
