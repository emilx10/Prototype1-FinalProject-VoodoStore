using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ContextBlocker : MonoBehaviour
{
    private static int ignoreCloseFrame = -1;

    [SerializeField] private GameObject contextMenu;
    [SerializeField] private List<GameObject> GOToEnable;
    [SerializeField] private List<Button> ButtonToEnable;
    [SerializeField] private GameManager gameManager;

    public static void IgnoreCloseForCurrentFrame()
    {
        ignoreCloseFrame = Time.frameCount;
    }

    public void CloseContext()
    {
        if (Time.frameCount == ignoreCloseFrame)
            return;
        if (IsPointerInsideContext())
            return;

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
            gameManager.CancelCrafting();
            gameManager.RefreshSellUI();
            
        }
    }

    private bool IsPointerInsideContext()
    {
        if (contextMenu == null)
            return false;

        RectTransform contextRoot = contextMenu.transform as RectTransform;
        if (contextRoot == null)
            return false;

        Canvas canvas = contextMenu.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        foreach (Transform child in contextMenu.transform)
        {
            if (!child.gameObject.activeInHierarchy)
                continue;

            RectTransform childRect = child as RectTransform;
            if (childRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(childRect, Input.mousePosition, eventCamera))
            {
                return true;
            }
        }

        return false;
    }
}
