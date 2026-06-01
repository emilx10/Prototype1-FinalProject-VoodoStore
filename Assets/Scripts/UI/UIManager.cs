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
    [SerializeField] private GameManager gameManager;

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

        gameManager.OpenSell(); // refresh sell items
    }

    public void OpenInventory()
    {
        marketPanel.SetActive(false);
        gameManager.OpenInventoryPanel();
    }

    public void OpenMerge()
    {
        mergePanel.SetActive(true);

        gameManager.OpenCrafting(); // refresh crafting items
    }

    public void OpenMarket()
    {
        CloseAll();
        gameManager.EndDay();
    }

    public void OpenItems()
    {
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