using UnityEngine;
using XRMultiplayer;
using UnityEngine.UI;
using Oculus.Avatar2;

public class HatShop : MonoBehaviour
{
    public Button hat0;
    public Button hat1;
    public Button hat2;
    public Button hat3;

    public HatSelector hatSelectorMirror;
      
    private void Start()
    {
        hat0.onClick.AddListener(() => SelectHat(0));
        hat1.onClick.AddListener(() => SelectHat(1));
        hat2.onClick.AddListener(() => SelectHat(2));
        hat3.onClick.AddListener(() => SelectHat(3));
    }

    public void SelectHat(int index)
    {
        hatSelectorMirror.SelectHat(index);

        BaseNetworkPlayer localPlayer = BaseNetworkPlayer.LocalPlayer;
        NetworkHatSelector hatSelector = localPlayer.GetComponent<NetworkHatSelector>();

        hatSelector.SelectHat(index);
    }
}
