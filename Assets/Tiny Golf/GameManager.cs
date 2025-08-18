using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private int currentHoleNumber = 0;

    public List<Transform> startingPositions;

    public Rigidbody ballRigidbody;

    public static GameManager Instance { get; private set; }
    private void Awake() => Instance = this;

    // Start is called before the first frame update
    void Start()
    {
        ballRigidbody.transform.position = startingPositions[currentHoleNumber].position;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
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
            currentHoleNumber = 0;
        }

        ballRigidbody.transform.position = startingPositions[currentHoleNumber].position;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
    }
}
