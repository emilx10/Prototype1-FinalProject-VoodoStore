using System.Collections;
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
    [SerializeField] private PageFoldScreenTransition screenTransition;

    private Coroutine openSellRoutine;
    private Coroutine openMarketRoutine;

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
        if (DelayForTransition(ref openSellRoutine, OpenSellAfterDelay()))
        {
            return;
        }

        OpenSellNow();
    }

    private IEnumerator OpenSellAfterDelay()
    {
        yield return new WaitForSecondsRealtime(GetTransitionDelay());
        openSellRoutine = null;
        OpenSellNow();
    }

    private void OpenSellNow()
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
        if (DelayForTransition(ref openMarketRoutine, OpenMarketAfterDelay()))
        {
            return;
        }

        OpenMarketNow();
    }

    private IEnumerator OpenMarketAfterDelay()
    {
        yield return new WaitForSecondsRealtime(GetTransitionDelay());
        openMarketRoutine = null;
        OpenMarketNow();
    }

    private void OpenMarketNow()
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

    private bool DelayForTransition(ref Coroutine routine, IEnumerator delayedAction)
    {
        float delay = GetTransitionDelay();
        if (delay <= 0f)
        {
            return false;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(delayedAction);
        return true;
    }

    private float GetTransitionDelay()
    {
        return screenTransition != null ? screenTransition.CoveredDelay : 0f;
    }
}
