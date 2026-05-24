using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ContextBlocker : MonoBehaviour
{
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private List<GameObject> GOToEnable;
    [SerializeField] private List<Button> ButtonToEnable;
    [SerializeField] private GameManager gameManager;
    public void CloseContext()
    {
        contextMenu.SetActive(false);
        gameObject.SetActive(false);
        foreach (GameObject go in GOToEnable)
        {
            go.SetActive(true);
        }

        foreach (Button b in ButtonToEnable)
        {
            b.interactable = true;
        }

        if (gameManager != null)
        {
            gameManager.RefreshSellUI();
        }
    }
}