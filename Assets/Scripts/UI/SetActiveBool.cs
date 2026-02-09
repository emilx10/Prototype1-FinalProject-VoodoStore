using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SetActiveBool : MonoBehaviour
{
    [SerializeField] GameObject mergePanel;
    [SerializeField] GameObject sellPanel;
    [SerializeField] GameObject insideShop;
    [SerializeField] List<GameObject> objectsToClose;

    public void CheckActiveMerge()
    {

        if (mergePanel.activeSelf)
        {
            foreach(var o in objectsToClose)
            {
                o.SetActive(false);
            }
        }
        else
        {
            foreach (var o in objectsToClose)
            {
                o.SetActive(true);
            }
        }
    }
    public void CheckActiveMarket()
    {

        if (insideShop.activeSelf)
        {
            foreach (var o in objectsToClose)
            {
                o.SetActive(false);
            }
        }
        else
        {
            foreach (var o in objectsToClose)
            {
                o.SetActive(true);
            }
        }
    }
    public void CheckActiveSell()
    {
        if (!sellPanel.activeSelf && !mergePanel.activeSelf)
        {
            foreach (var o in objectsToClose)
            {
                o.SetActive(true);
            }
        }
        else
        {
            foreach (var o in objectsToClose)
            {
                o.SetActive(false);
            }
        }
    }
}
