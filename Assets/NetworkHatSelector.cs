using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class NetworkHatSelector : NetworkBehaviour
{
    [System.Serializable]
    public class Hat
    {
        public GameObject hatObject;
    }

    public List<Hat> hats = new List<Hat>();

    private readonly NetworkVariable<int> _selectedHatIndex =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        ApplyHatVisuals(0);
    }

    public override void OnNetworkSpawn()
    {
        ApplyHatVisuals(_selectedHatIndex.Value);

        // React to future changes
        _selectedHatIndex.OnValueChanged += OnHatIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        _selectedHatIndex.OnValueChanged -= OnHatIndexChanged;
    }

    private void OnHatIndexChanged(int oldValue, int newValue)
    {
        ApplyHatVisuals(newValue);
    }

    public void SelectHat(int index)
    {
        if (IsOwner)
        {
            _selectedHatIndex.Value = index;
        }
    }

    private void ApplyHatVisuals(int index)
    {
        for (int i = 0; i < hats.Count; i++)
        {
            var go = hats[i].hatObject;
            if (go != null) 
                go.SetActive(i == index);
        }
    }
}
