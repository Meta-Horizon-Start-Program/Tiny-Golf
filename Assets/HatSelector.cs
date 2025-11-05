using System.Collections.Generic;
using UnityEngine;

public class HatSelector : MonoBehaviour
{
    [System.Serializable]
    public class Hat
    {
        public GameObject hatObject;
    }

    public List<Hat> hats = new List<Hat>();

    void Start()
    {
        SelectHat(0); // Select first hat by default
    }

    public void SelectHat(int index)
    {
        for (int i = 0; i < hats.Count; i++)
        {
            if (hats[i].hatObject != null)
                hats[i].hatObject.SetActive(i == index);
        }

        Debug.Log("Selected hat: " + hats[index]);
    }
}
