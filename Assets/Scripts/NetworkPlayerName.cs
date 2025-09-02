using UnityEngine;
using TMPro;
using Meta.XR.MultiplayerBlocks.Shared;
using Unity.Netcode;
using Unity.Collections;

public class NetworkPlayerName : NetworkBehaviour
{
    public TextMeshPro nameLabel;

    [SerializeField] bool m_FlipForward;
    protected Camera m_Camera;

    private NetworkVariable<FixedString64Bytes> PlayerName
        = new NetworkVariable<FixedString64Bytes>(
            default,
             NetworkVariableReadPermission.Everyone,
              NetworkVariableWritePermission.Owner);

    // Fallback pool of random names
    public string[] RandomNames = new string[]
    {
        "Paul", "Jean", "Pierre", "Marie", "Luc", "Anna", "Sophie", "Louis"
    };

    private void Awake()
    {
        m_Camera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        SetPlayerNameText(PlayerName.Value.ToString());

        PlayerName.OnValueChanged += OnNameChanged;

        if(IsOwner)
        {
            PlatformInit.GetEntitlementInformation(OnEntitlementFinished);
        }
    }

    public void OnNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetPlayerNameText(newValue.ToString());
    }

    private void LateUpdate()
    {
        UpdateRotation();
    }

    public void UpdateRotation()
    {
        if (m_Camera == null)
            return;

        Quaternion lookRot = Quaternion.LookRotation(m_Camera.transform.position - transform.position);

        Vector3 offset = lookRot.eulerAngles;
        offset.x = 0;
        offset.z = 0;

        if (m_FlipForward)
            offset.y += 180;

        lookRot = Quaternion.Euler(offset);

        transform.rotation = lookRot;
    }

    private void OnEntitlementFinished(PlatformInfo info)
    {
        string playerName = null;

        if (info.IsEntitled && !string.IsNullOrEmpty(info.OculusUser?.DisplayName))
        {
            playerName = info.OculusUser.DisplayName;
        }
        else
        {
            playerName = GetRandomName();
        }

        PlayerName.Value = playerName;
    }

    private void SetPlayerNameText(string value)
    {
        if (nameLabel != null)
            nameLabel.text = value;
    }

    private string GetRandomName()
    {
        return RandomNames[Random.Range(0, RandomNames.Length)];
    }
}
