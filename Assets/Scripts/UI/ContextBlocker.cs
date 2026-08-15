using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ContextBlocker : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, ICanvasRaycastFilter
{
    private static int ignoreCloseFrame = -1;
    private const bool DebugClicks = true;

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

        LogClick("Update/Input.GetMouseButtonDown", Input.mousePosition, null);
        CloseContext();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LogClick("OnPointerClick", eventData.position, eventData.pointerCurrentRaycast.gameObject);
        CloseContext();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        LogClick("OnPointerDown", eventData.position, eventData.pointerCurrentRaycast.gameObject);
        CloseContext();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        bool inside = IsScreenPointInsideContext(screenPoint, eventCamera);
        if (DebugClicks && Input.GetMouseButtonDown(0))
        {
            Debug.Log(
                $"[ContextBlocker:{name}] RaycastFilter point={screenPoint} context={GetObjectName(contextMenu)} " +
                $"crafting={IsCraftingContext()} inside={inside} validForBlocker={!inside}",
                this);
        }

        return !inside;
    }

    public void CloseContext()
    {
        if (Time.frameCount == ignoreCloseFrame)
        {
            DebugClose("ignored because IgnoreCloseForCurrentFrame is active");
            return;
        }

        bool isCraftingContext = gameManager != null &&
            gameManager.craftingPanel != null &&
            TargetsContext(gameManager.craftingPanel);

        if (gameManager != null)
        {
            if (!isCraftingContext &&
                (gameManager.IsKnownRecipesOpen() || IsPointerOverBookControl()))
            {
                DebugClose("blocked by known recipes/book controls");
                return;
            }
        }

        bool insideContext = IsPointerInsideContext();
        if (insideContext)
        {
            DebugClose("blocked because click is inside protected context");
            return;
        }

        DebugClose("closing context");

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
        {
            DebugInside("no contextMenu assigned", screenPoint, false);
            return false;
        }

        RectTransform contextRoot = contextMenu.transform as RectTransform;
        if (contextRoot == null)
        {
            DebugInside("contextMenu has no RectTransform", screenPoint, false);
            return false;
        }

        Canvas canvas = contextMenu.GetComponentInParent<Canvas>();
        Camera contextCamera = eventCamera;
        if (contextCamera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            contextCamera = canvas.worldCamera;

        if (IsCraftingContext())
        {
            bool insideCauldron = IsScreenPointInsideCauldron(screenPoint, contextCamera);
            DebugInside("crafting uses cauldron boundary", screenPoint, insideCauldron);
            return insideCauldron;
        }

        foreach (Transform child in contextMenu.transform)
        {
            if (!child.gameObject.activeInHierarchy)
                continue;

            RectTransform childRect = child as RectTransform;
            if (childRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(childRect, screenPoint, contextCamera))
            {
                DebugInside($"inside child rect '{child.name}'", screenPoint, true);
                return true;
            }
        }

        DebugInside("outside all context child rects", screenPoint, false);
        return false;
    }

    private bool IsCraftingContext()
    {
        if (gameManager != null &&
            gameManager.craftingPanel != null &&
            TargetsContext(gameManager.craftingPanel))
            return true;

        return contextMenu != null && contextMenu.name.Trim() == "CraftingPanel";
    }

    private bool IsScreenPointInsideCauldron(Vector2 screenPoint, Camera eventCamera)
    {
        Transform cauldron = FindChildByTrimmedName(contextMenu.transform, "Cauldron");
        if (cauldron == null)
        {
            if (DebugClicks)
                Debug.Log($"[ContextBlocker:{name}] Cauldron boundary NOT FOUND under context={GetObjectName(contextMenu)}", this);
            return false;
        }

        bool insideRendererBounds = IsScreenPointInsideRendererBounds(cauldron, screenPoint, eventCamera, DebugClicks, this);
        if (DebugClicks)
        {
            Debug.Log(
                $"[ContextBlocker:{name}] Cauldron visual renderer bounds result={insideRendererBounds} " +
                $"point={screenPoint} cauldron='{cauldron.name}' camera={GetObjectName(eventCamera != null ? eventCamera.gameObject : null)}",
                this);
        }

        return insideRendererBounds;
    }

    private static bool IsScreenPointInsideRendererBounds(
        Transform root,
        Vector2 screenPoint,
        Camera eventCamera,
        bool debug,
        Object debugContext)
    {
        Camera camera = eventCamera != null ? eventCamera : Camera.main;
        if (camera == null || root == null)
        {
            if (debug)
                Debug.Log($"[ContextBlocker] RendererBounds no camera/root camera={GetObjectName(camera != null ? camera.gameObject : null)} root={GetObjectName(root != null ? root.gameObject : null)}", debugContext);
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        bool foundBounds = false;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 screen = camera.WorldToScreenPoint(corner);
                        if (screen.z < 0f)
                            continue;

                        foundBounds = true;
                        min = Vector2.Min(min, screen);
                        max = Vector2.Max(max, screen);
                    }
                }
            }
        }

        if (!foundBounds)
        {
            if (debug)
                Debug.Log($"[ContextBlocker] RendererBounds no enabled renderers under {GetObjectName(root.gameObject)}", debugContext);
            return false;
        }

        bool inside = screenPoint.x >= min.x &&
            screenPoint.x <= max.x &&
            screenPoint.y >= min.y &&
            screenPoint.y <= max.y;

        if (debug)
            Debug.Log($"[ContextBlocker] RendererBounds min={min} max={max} point={screenPoint} inside={inside}", debugContext);

        return inside;
    }

    private static Transform FindChildByTrimmedName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            if (child.name.Trim() == targetName)
                return child;

            Transform nested = FindChildByTrimmedName(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void LogClick(string source, Vector2 point, GameObject raycastObject)
    {
        if (!DebugClicks)
            return;

        Debug.Log(
            $"[ContextBlocker:{name}] {source} point={point} raycast={GetObjectName(raycastObject)} " +
            $"activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy} " +
            $"context={GetObjectName(contextMenu)} contextActive={(contextMenu != null && contextMenu.activeInHierarchy)} " +
            $"gameManager={GetObjectName(gameManager != null ? gameManager.gameObject : null)}",
            this);
    }

    private void DebugClose(string reason)
    {
        if (!DebugClicks)
            return;

        Debug.Log(
            $"[ContextBlocker:{name}] CloseContext {reason}. " +
            $"mouse={Input.mousePosition} context={GetObjectName(contextMenu)} crafting={IsCraftingContext()} frame={Time.frameCount}",
            this);
    }

    private void DebugInside(string reason, Vector2 point, bool inside)
    {
        if (!DebugClicks)
            return;

        Debug.Log(
            $"[ContextBlocker:{name}] InsideCheck {reason}. point={point} inside={inside} " +
            $"context={GetObjectName(contextMenu)} crafting={IsCraftingContext()}",
            this);
    }

    private static string GetObjectName(Object target)
    {
        return target != null ? target.name : "null";
    }
}
