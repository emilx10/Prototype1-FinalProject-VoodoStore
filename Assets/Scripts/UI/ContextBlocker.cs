using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ContextBlocker : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, ICanvasRaycastFilter
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

    public bool TargetsContext(GameObject target)
    {
        return contextMenu == target;
    }

    public void AssignGameManagerIfMissing(GameManager manager)
    {
        if (gameManager == null)
            gameManager = manager;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        CloseContext();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        CloseContext();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CloseContext();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        return !IsScreenPointInsideContext(screenPoint, eventCamera);
    }

    public void CloseContext()
    {
        if (Time.frameCount == ignoreCloseFrame)
            return;
        bool isCraftingContext = gameManager != null &&
            gameManager.craftingPanel != null &&
            TargetsContext(gameManager.craftingPanel);

        if (gameManager != null)
        {
            if (!isCraftingContext &&
                (gameManager.IsKnownRecipesOpen() || IsPointerOverBookControl()))
                return;
        }

        if (IsPointerInsideContext())
            return;

        if (contextMenu != null)
            contextMenu.SetActive(false);

        gameObject.SetActive(false);
        
        if (GOToEnable != null)
        {
            foreach (GameObject go in GOToEnable)
            {
                if (go != null)
                    go.SetActive(true);
            }
        }

        if (ButtonToEnable != null)
        {
            foreach (Button b in ButtonToEnable)
            {
                if (b != null)
                    b.interactable = true;
            }
        }

        if (gameManager != null)
        {
            gameManager.CancelCrafting();
            gameManager.RefreshSellUI();
            
        }
    }

    private bool IsPointerOverBookControl()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Transform current = result.gameObject.transform;
            while (current != null)
            {
                if (current.name == "BookCanvas")
                    return true;

                current = current.parent;
            }
        }

        return false;
    }

    private bool IsPointerInsideContext()
    {
        return IsScreenPointInsideContext(Input.mousePosition, null);
    }

    private bool IsScreenPointInsideContext(Vector2 screenPoint, Camera eventCamera)
    {
        if (contextMenu == null)
            return false;

        RectTransform contextRoot = contextMenu.transform as RectTransform;
        if (contextRoot == null)
            return false;

        Canvas canvas = contextMenu.GetComponentInParent<Canvas>();
        Camera contextCamera = eventCamera;
        if (contextCamera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            contextCamera = canvas.worldCamera;

        foreach (Transform child in contextMenu.transform)
        {
            if (!child.gameObject.activeInHierarchy)
                continue;

            RectTransform childRect = child as RectTransform;
            if (childRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(childRect, screenPoint, contextCamera))
            {
                return true;
            }
        }

        return false;
    }
}
