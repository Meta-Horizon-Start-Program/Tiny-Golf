using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private int currentHoleNumber = 0;

    public List<Transform> startingPositions;

    public Rigidbody ballRigidbody;

    public TMPro.TextMeshPro textmesh;

    public int currentHitNumber = 0;
    private List<int> previousHitNumbers = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        ballRigidbody.transform.position = startingPositions[currentHoleNumber].position;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        textmesh.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GoToNextHole();
        }
    }

    public void GoToNextHole()
    {
        currentHoleNumber += 1;

        if(currentHoleNumber >= startingPositions.Count)
        {
            Debug.Log("We reached the end");
        }
        else
        {
            ballRigidbody.transform.position = startingPositions[currentHoleNumber].position;

            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
        }

        previousHitNumbers.Add(currentHitNumber);
        currentHitNumber = 0;
        DisplayScore();
    }

    public void DisplayScore()
    {
        string scoreText = "";

        for (int i = 0; i < previousHitNumbers.Count; i++)
        {
            scoreText += "HOLE " + (i + 1) + " - " + previousHitNumbers[i] + "<br>";
        }

        textmesh.text = scoreText;
    }
}
