using UnityEngine;
using XRMultiplayer;

public class NetworkUI : MonoBehaviour
{
    public GameObject lobbyUI;
    public GameObject roomUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lobbyUI.SetActive(true);
        roomUI.SetActive(false);

        XRINetworkGameManager.CurrentConnectionState.Subscribe(ConnectionStateChanged);
    }

    public void ConnectionStateChanged(XRINetworkGameManager.ConnectionState state)
    {
        if(state == XRINetworkGameManager.ConnectionState.Connected)
        {
            lobbyUI.SetActive(false);
            roomUI.SetActive(true);
        }
        else
        {
            lobbyUI.SetActive(true);
            roomUI.SetActive(false);
        }
    }
}
