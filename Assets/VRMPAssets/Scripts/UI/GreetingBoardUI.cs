using TMPro;
using UnityEngine;
using XRMultiplayer;

public class GreetingBoardUI : MonoBehaviour
{
    [SerializeField] TMP_Text m_RoomNameText;
    [SerializeField] TMP_Text m_RoomCodeText;


    private void OnEnable()
    {
        XRINetworkGameManager.Connected.Subscribe(ConnectedToGame);
        XRINetworkGameManager.ConnectedRoomName.Subscribe(UpdateRoomName);
    }

    private void OnDisable()
    {
        XRINetworkGameManager.Connected.Unsubscribe(ConnectedToGame);
        XRINetworkGameManager.ConnectedRoomName.Unsubscribe(UpdateRoomName);
    }

    void ConnectedToGame(bool connected)
    {
        if (connected)
        {
            m_RoomNameText.text = XRINetworkGameManager.ConnectedRoomName.Value;
            m_RoomCodeText.text = XRINetworkGameManager.ConnectedRoomCode;
        }
    }

    void UpdateRoomName(string roomName)
    {
        m_RoomNameText.text = roomName;
    }
}
