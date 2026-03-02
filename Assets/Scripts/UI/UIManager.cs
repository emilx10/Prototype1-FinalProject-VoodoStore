using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject sellPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject mergePanel;
    [SerializeField] private GameObject marketPanel;
    [SerializeField] private GameObject itemsPanel;
    [SerializeField] private List<GameObject> objectsToClose;

    void CloseAll()
    {
        sellPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        mergePanel.SetActive(false);
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
    }

    public void OpenSell()
    {
        CloseAll();
        sellPanel.SetActive(true);
    }

    public void OpenInventory()
    {
        marketPanel.SetActive(false);
        inventoryPanel.SetActive(true);
    }

    public void OpenMerge()
    {
        CloseAll();
        mergePanel.SetActive(true);
    }

    public void OpenMarket()
    {
        CloseAll();
        marketPanel.SetActive(true);
    }

    public void OpenItems()
    {
        CloseAll();
        itemsPanel.SetActive(true);
    }

    public void CloseEverything()
    {
        CloseAll();
    }

    public void CheckActive()
    {
        if (sellPanel.activeInHierarchy || mergePanel.activeInHierarchy || itemsPanel.activeInHierarchy)
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
}