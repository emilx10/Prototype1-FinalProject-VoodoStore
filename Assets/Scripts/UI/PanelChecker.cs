using System.Collections.Generic;
using UnityEngine;

public class PanelChecker : MonoBehaviour
{
    [Header("Panels To Check")]
    [SerializeField] private GameObject mergePanel;
    [SerializeField] private GameObject sellPanel;
    [SerializeField] private GameObject insideShop;

    [Header("Objects To Hide")]
    [SerializeField] private List<GameObject> objectsToToggle;

    public void CheckPanels()
    {
        bool anyPanelActive =
            (mergePanel != null && mergePanel.activeInHierarchy) ||
            (sellPanel != null && sellPanel.activeInHierarchy) ||
            (insideShop != null && insideShop.activeInHierarchy);

        Debug.Log("Merge: " + mergePanel.activeInHierarchy);
        Debug.Log("Sell: " + sellPanel.activeInHierarchy);
        Debug.Log("Market: " + insideShop.activeInHierarchy);
        Debug.Log("Any Active: " + anyPanelActive);

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                Debug.Log("Toggling: " + obj.name + " -> " + !anyPanelActive);
                obj.SetActive(!anyPanelActive);
            }
        }
    }
}