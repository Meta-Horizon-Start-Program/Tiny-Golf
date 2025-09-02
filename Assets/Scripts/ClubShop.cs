using Oculus.Platform;
using Oculus.Platform.Models;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;

public class ClubShop : MonoBehaviour
{
    public string[] skus = new[] {"golden-ball"};

    public void Start()
    {
        XRINetworkGameManager.CurrentConnectionState.Subscribe(Initialize);
    }

    public void Initialize(XRINetworkGameManager.ConnectionState state)
    {
        if (state == XRINetworkGameManager.ConnectionState.Connected)
        {
            GetPrices();
            GetPurchases();
        }
    }

    private void GetPrices()
    {
        IAP.GetProductsBySKU(skus).OnComplete(GetProductsCompleted);
    }

    private void GetProductsCompleted(Message<ProductList> msg)
    {
        if (msg.IsError)
        {
            Debug.Log("Fail" + msg.GetError().ToString());
            return;
        }

        string text = "Available Products : " + "\n";

        foreach (var prod in msg.GetProductList())
        {
            text += prod.Name + "-" + prod.FormattedPrice + "\n";
        }

        Debug.Log(text);
    }

    private void GetPurchases()
    {
        IAP.GetViewerPurchases().OnComplete(GetPurchasesCompleted);
    }

    private void GetPurchasesCompleted(Message<PurchaseList> msg)
    {
        if (msg.IsError)
        {
            Debug.Log("Fail" + msg.GetError().ToString());
            return;
        }

        string text = "Purchased Products : " + "\n";

        foreach (var purch in msg.GetPurchaseList())
        {
            text += purch.Sku + "-" + purch.GrantTime + "\n";

            if(purch.Sku == "golden-ball-durable")
            {
                GameManager.Instance.RespawnWithGoldenBall();
            }
        }

        Debug.Log(text);
    }

    public void BuyGoldenBall()
    {
        IAP.LaunchCheckoutFlow("golden-ball-durable").OnComplete(BuyGoldenBallCallback);
    }

    private void BuyGoldenBallCallback(Message<Purchase> msg)
    {
        if (msg.IsError)
            return;

        GameManager.Instance.RespawnWithGoldenBall();
    }
}
