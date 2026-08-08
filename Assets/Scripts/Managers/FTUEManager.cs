using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Owns tutorial progress for the lifetime of the running game only.
/// The object survives scene reloads, but nothing is written to PlayerPrefs or disk.
/// </summary>
public sealed class FTUEManager : MonoBehaviour
{
    private enum TutorialState
    {
        Idle,
        ShowingPopup,
        WaitingForKnownRecipesIconClick
    }

    private const int DimSortingOrder = 32000;
    private const int HighlightSortingOrder = 32001;
    private const int InputSortingOrder = 32002;
    private const int PopupSortingOrder = 32003;
    private const float DismissDelay = 3f;

    private static FTUEManager instance;

    public bool HasShownObjectivesFTUE { get; private set; }
    public bool HasShownKnownRecipesFTUE { get; private set; }
    public bool HasShownNightDayFTUE { get; private set; }
    public bool HasShownInventoryFTUE { get; private set; }

    private bool hasPurchasedIngredient;
    private bool tutorialActive;
    private bool knownRecipesIconClicked;
    private bool initialFTUEComplete;
    private TutorialState state;
    private RectTransform objectivesPanel;
    private RectTransform knownRecipesButton;
    private RectTransform knownRecipesPanel;
    private RectTransform visibleInventoryPanel;
    private ButtonBreather bookAttentionAnimation;
    private ButtonBreather marketAttentionAnimation;
    private bool marketAttentionWasRunning;
    private Button marketButton;
    private bool marketButtonWasInteractable;
    private bool marketRestrictionActive;

    private Canvas dimCanvas;
    private Canvas inputCanvas;
    private Canvas popupCanvas;
    private GameObject popupRoot;
    private GameObject clickIndicator;
    private FTUEClickCatcher clickCatcher;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private RectTransform popupRect;

    private RectTransform highlightedTarget;
    private Canvas highlightCanvas;
    private GraphicRaycaster highlightRaycaster;
    private bool addedHighlightCanvas;
    private bool addedHighlightRaycaster;
    private bool previousRaycasterEnabled;
    private CanvasGroup allowedTargetGroup;
    private Graphic allowedTargetGraphic;
    private bool addedAllowedTargetGroup;
    private bool previousGroupInteractable;
    private bool previousGroupBlocksRaycasts;
    private bool previousGroupIgnoreParents;
    private bool previousButtonEnabled;
    private bool previousButtonInteractable;
    private bool previousGraphicRaycastTarget;
    private Transform allowedTargetOriginalParent;
    private int allowedTargetOriginalSiblingIndex;
    private Vector2 allowedTargetOriginalAnchorMin;
    private Vector2 allowedTargetOriginalAnchorMax;
    private Vector2 allowedTargetOriginalPivot;
    private Vector2 allowedTargetOriginalAnchoredPosition;
    private Vector2 allowedTargetOriginalSizeDelta;
    private Vector3 allowedTargetOriginalScale;
    private Quaternion allowedTargetOriginalRotation;
    private bool allowedTargetMovedToOverlay;
    private bool hasInitialPopupPosition;
    private Vector2 initialPopupPosition;
    private bool previousOverrideSorting;
    private int previousSortingOrder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject managerObject = new GameObject("FTUE Manager");
        instance = managerObject.AddComponent<FTUEManager>();
        DontDestroyOnLoad(managerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        GameManager.OnIngredientPurchased += HandleIngredientPurchased;
    }

    private void OnDestroy()
    {
        GameManager.OnIngredientPurchased -= HandleIngredientPurchased;
        if (instance == this)
            instance = null;
    }

    public static void NotifyObjectivesOpened(
        RectTransform panel,
        RectTransform recipesButton,
        ButtonBreather bookBreather,
        ButtonBreather marketBreather)
    {
        EnsureInstance();
        if (instance == null || instance.tutorialActive || instance.HasShownObjectivesFTUE)
            return;

        instance.objectivesPanel = panel;
        instance.knownRecipesButton = recipesButton;
        instance.bookAttentionAnimation = bookBreather;
        instance.marketAttentionAnimation = marketBreather;
        instance.StartCoroutine(instance.RunInitialSequence());
    }

    public static void RegisterMarketControl(Button button, ButtonBreather breather)
    {
        EnsureInstance();
        if (instance == null || button == null || instance.initialFTUEComplete)
            return;

        instance.marketButton = button;
        instance.marketAttentionAnimation = breather;

        if (!instance.marketRestrictionActive)
        {
            instance.marketButtonWasInteractable = button.interactable;
            instance.marketAttentionWasRunning = breather != null && breather.IsBreathing;
            instance.marketRestrictionActive = true;
        }

        button.interactable = false;
        if (breather != null)
            breather.PauseBreathing();
    }

    public static void NotifyInventoryOpened(RectTransform inventoryPanel)
    {
        EnsureInstance();
        if (instance == null || inventoryPanel == null)
            return;

        instance.visibleInventoryPanel = inventoryPanel;
        instance.TryStartInventoryTutorial();
    }

    public static void NotifyInventoryClosed(RectTransform inventoryPanel)
    {
        EnsureInstance();
        if (instance != null && instance.visibleInventoryPanel == inventoryPanel)
            instance.visibleInventoryPanel = null;
    }

    public static void NotifyKnownRecipesOpened(RectTransform panel)
    {
        EnsureInstance();
        if (instance == null || instance.state != TutorialState.WaitingForKnownRecipesIconClick)
            return;

        Debug.Log("FTUE: Known Recipes potion icon clicked");
        instance.knownRecipesPanel = panel;
        instance.knownRecipesIconClicked = true;
    }

    private void HandleIngredientPurchased()
    {
        hasPurchasedIngredient = true;
        Debug.Log("FTUE: First ingredient purchase detected; Inventory tutorial is ready.");
        TryStartInventoryTutorial();
    }

    private void TryStartInventoryTutorial()
    {
        if (tutorialActive || HasShownInventoryFTUE || !hasPurchasedIngredient ||
            visibleInventoryPanel == null || !visibleInventoryPanel.gameObject.activeInHierarchy)
        {
            return;
        }

        Debug.Log("FTUE: Starting Inventory tutorial.");
        StartCoroutine(RunInventoryTutorial(visibleInventoryPanel));
    }

    private IEnumerator RunInitialSequence()
    {
        tutorialActive = true;
        PauseMarketAttentionAnimation();

        HasShownObjectivesFTUE = true;
        yield return ShowStep(
            objectivesPanel,
            "This is the Objectives panel.",
            "Your objectives will guide you through the game, especially during the first few days. If you're ever unsure what to do next, check this panel for your current goal and the steps needed to complete it.",
            true);

        yield return WaitForKnownRecipesIconClick();

        HasShownKnownRecipesFTUE = true;
        yield return ShowStep(
            knownRecipesPanel,
            "This is the Known Recipes menu.",
            "Every product in the game is listed here. Each recipe requires three ingredients. The colored glow around each ? hints at the ingredient's color.",
            true);

        DayNightCycleUI clock = FindFirstObjectByType<DayNightCycleUI>();
        HasShownNightDayFTUE = true;
        yield return ShowStep(
            clock != null ? clock.transform as RectTransform : null,
            "This is the Night & Day icon.",
            "Each day is divided into three phases, each with its own main activity. Every new day brings you one step closer to the 20-day deadline.",
            true);

        initialFTUEComplete = true;
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
            gameManager.SetBrewButtonVisibleForGameplayPhase(true);
        ResumeMarketAttentionAnimation();
        EndTutorial();
    }

    private void PauseMarketAttentionAnimation()
    {
        if (marketRestrictionActive)
            return;

        if (marketAttentionAnimation == null)
            return;

        marketAttentionWasRunning = marketAttentionAnimation.IsBreathing;
        marketAttentionAnimation.PauseBreathing();
        marketRestrictionActive = true;
    }

    private void ResumeMarketAttentionAnimation()
    {
        if (marketButton != null)
            marketButton.interactable = marketButtonWasInteractable;

        if (marketAttentionAnimation != null && marketAttentionWasRunning)
            marketAttentionAnimation.ResumeBreathing();

        marketButton = null;
        marketAttentionAnimation = null;
        marketButtonWasInteractable = false;
        marketAttentionWasRunning = false;
        marketRestrictionActive = false;
    }

    private IEnumerator WaitForKnownRecipesIconClick()
    {
        state = TutorialState.WaitingForKnownRecipesIconClick;
        knownRecipesIconClicked = false;

        Highlight(knownRecipesButton);
        MoveAllowedTargetToOverlay(knownRecipesButton);
        Button potionButton = knownRecipesButton != null ? knownRecipesButton.GetComponent<Button>() : null;
        ButtonBreather potionAttentionAnimation = StartPotionAttentionAnimation(potionButton);
        if (potionButton != null)
        {
            EnableAllowedButtonInteraction(potionButton);
            potionButton.onClick.AddListener(DebugKnownRecipesPotionClick);
        }

        popupCanvas.gameObject.SetActive(false);
        dimCanvas.gameObject.SetActive(true);
        inputCanvas.sortingOrder = DimSortingOrder;
        inputCanvas.gameObject.SetActive(true);
        clickCatcher.SetDismissEnabled(false);
        clickCatcher.SetAllowedTarget(knownRecipesButton);

        if (potionButton != null)
        {
            Debug.Log(
                $"FTUE waiting for potion click. " +
                $"Button enabled: {potionButton.enabled}, " +
                $"Interactable: {potionButton.interactable}, " +
                $"Active: {potionButton.gameObject.activeInHierarchy}");
            LogParentCanvasGroups(potionButton.transform);
        }
        else
        {
            Debug.LogError("FTUE: The serialized Known Recipes potion target has no Button component.");
        }

        Debug.Log($"FTUE EventSystem active: {EventSystem.current != null && EventSystem.current.isActiveAndEnabled}");

        yield return new WaitUntil(() => knownRecipesIconClicked);

        if (potionButton != null)
        {
            potionButton.onClick.RemoveListener(DebugKnownRecipesPotionClick);
            RestoreAllowedButtonInteraction(potionButton);
        }
        StopPotionAttentionAnimation(potionAttentionAnimation);
        RestoreAllowedTargetParent(knownRecipesButton);
        clickCatcher.SetAllowedTarget(null);
        inputCanvas.sortingOrder = InputSortingOrder;
        RemoveHighlight();
        state = TutorialState.ShowingPopup;
    }

    public void DebugKnownRecipesPotionClick()
    {
        Debug.Log("FTUE DEBUG: Potion icon received click");
    }

    private ButtonBreather StartPotionAttentionAnimation(Button potionButton)
    {
        if (potionButton == null || bookAttentionAnimation == null)
            return null;

        ButtonBreather potionBreather = potionButton.GetComponent<ButtonBreather>();
        bool addedForTutorial = potionBreather == null;
        if (addedForTutorial)
            potionBreather = potionButton.gameObject.AddComponent<ButtonBreather>();

        potionBreather.speed = bookAttentionAnimation.speed;
        potionBreather.scaleAmount = bookAttentionAnimation.scaleAmount;
        potionBreather.playOnStart = addedForTutorial;
        potionBreather.StartBreathing();
        return potionBreather;
    }

    private void StopPotionAttentionAnimation(ButtonBreather potionBreather)
    {
        if (potionBreather == null)
            return;

        potionBreather.StopBreathing();

        // The Potion button does not normally own this behavior; remove the
        // temporary reused component after it has reset the target scale.
        if (potionBreather != bookAttentionAnimation)
            Destroy(potionBreather);
    }

    private void EnableAllowedButtonInteraction(Button button)
    {
        previousButtonEnabled = button.enabled;
        previousButtonInteractable = button.interactable;
        button.enabled = true;
        button.interactable = true;

        allowedTargetGraphic = button.targetGraphic;
        if (allowedTargetGraphic != null)
        {
            previousGraphicRaycastTarget = allowedTargetGraphic.raycastTarget;
            allowedTargetGraphic.raycastTarget = true;
        }

        allowedTargetGroup = button.GetComponent<CanvasGroup>();
        if (allowedTargetGroup == null)
        {
            allowedTargetGroup = button.gameObject.AddComponent<CanvasGroup>();
            addedAllowedTargetGroup = true;
        }
        else
        {
            previousGroupInteractable = allowedTargetGroup.interactable;
            previousGroupBlocksRaycasts = allowedTargetGroup.blocksRaycasts;
            previousGroupIgnoreParents = allowedTargetGroup.ignoreParentGroups;
        }

        allowedTargetGroup.interactable = true;
        allowedTargetGroup.blocksRaycasts = true;
        allowedTargetGroup.ignoreParentGroups = true;
    }

    private void RestoreAllowedButtonInteraction(Button button)
    {
        button.enabled = previousButtonEnabled;
        button.interactable = previousButtonInteractable;
        if (allowedTargetGraphic != null)
            allowedTargetGraphic.raycastTarget = previousGraphicRaycastTarget;

        if (addedAllowedTargetGroup && allowedTargetGroup != null)
            Destroy(allowedTargetGroup);
        else if (allowedTargetGroup != null)
        {
            allowedTargetGroup.interactable = previousGroupInteractable;
            allowedTargetGroup.blocksRaycasts = previousGroupBlocksRaycasts;
            allowedTargetGroup.ignoreParentGroups = previousGroupIgnoreParents;
        }

        allowedTargetGroup = null;
        allowedTargetGraphic = null;
        addedAllowedTargetGroup = false;
    }

    private void MoveAllowedTargetToOverlay(RectTransform target)
    {
        if (target == null || dimCanvas == null || allowedTargetMovedToOverlay)
            return;

        Canvas sourceCanvas = target.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera
            : null;

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);
        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldCorners[0]);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldCorners[2]);
        Vector2 centerScreen = (bottomLeftScreen + topRightScreen) * 0.5f;

        RectTransform overlayRect = dimCanvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, bottomLeftScreen, null, out Vector2 bottomLeftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, topRightScreen, null, out Vector2 topRightLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, centerScreen, null, out Vector2 centerLocal);

        allowedTargetOriginalParent = target.parent;
        allowedTargetOriginalSiblingIndex = target.GetSiblingIndex();
        allowedTargetOriginalAnchorMin = target.anchorMin;
        allowedTargetOriginalAnchorMax = target.anchorMax;
        allowedTargetOriginalPivot = target.pivot;
        allowedTargetOriginalAnchoredPosition = target.anchoredPosition;
        allowedTargetOriginalSizeDelta = target.sizeDelta;
        allowedTargetOriginalScale = target.localScale;
        allowedTargetOriginalRotation = target.localRotation;

        target.SetParent(overlayRect, false);
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = centerLocal;
        target.sizeDelta = new Vector2(
            Mathf.Abs(topRightLocal.x - bottomLeftLocal.x),
            Mathf.Abs(topRightLocal.y - bottomLeftLocal.y));
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
        allowedTargetMovedToOverlay = true;
    }

    private void RestoreAllowedTargetParent(RectTransform target)
    {
        if (!allowedTargetMovedToOverlay || target == null || allowedTargetOriginalParent == null)
            return;

        target.SetParent(allowedTargetOriginalParent, false);
        target.SetSiblingIndex(Mathf.Min(allowedTargetOriginalSiblingIndex, target.parent.childCount - 1));
        target.anchorMin = allowedTargetOriginalAnchorMin;
        target.anchorMax = allowedTargetOriginalAnchorMax;
        target.pivot = allowedTargetOriginalPivot;
        target.anchoredPosition = allowedTargetOriginalAnchoredPosition;
        target.sizeDelta = allowedTargetOriginalSizeDelta;
        target.localScale = allowedTargetOriginalScale;
        target.localRotation = allowedTargetOriginalRotation;

        allowedTargetOriginalParent = null;
        allowedTargetMovedToOverlay = false;
    }

    private static void LogParentCanvasGroups(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null)
            {
                Debug.Log(
                    $"FTUE CanvasGroup '{current.name}': interactable={group.interactable}, " +
                    $"blocksRaycasts={group.blocksRaycasts}, ignoreParentGroups={group.ignoreParentGroups}");
            }
            current = current.parent;
        }
    }

    internal static void LogRaycastHits(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("FTUE Raycast: no active EventSystem.");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
            Debug.Log($"FTUE Raycast hit: {results[i].gameObject.name}");
    }

    private IEnumerator RunInventoryTutorial(RectTransform inventoryPanel)
    {
        tutorialActive = true;
        HasShownInventoryFTUE = true;

        yield return ShowStep(
            inventoryPanel,
            "This is your Inventory.",
            "Once per day, click and hold the icon beside an ingredient to reveal one recipe that uses it in the Known Recipes menu.");

        EndTutorial();
    }

    private IEnumerator ShowStep(RectTransform target, string title, string body, bool useInitialPopupPosition = false)
    {
        state = TutorialState.ShowingPopup;
        EnsureTutorialUI();
        Highlight(target);
        MoveAllowedTargetToOverlay(target);
        if (useInitialPopupPosition)
        {
            if (!hasInitialPopupPosition)
            {
                PositionPopupAwayFrom(target);
                initialPopupPosition = popupRect.anchoredPosition;
                hasInitialPopupPosition = true;
            }
            else
            {
                popupRect.anchoredPosition = initialPopupPosition;
            }
        }
        else
        {
            PositionPopupAwayFrom(target);
        }

        titleText.text = title;
        bodyText.text = body;
        clickIndicator.SetActive(false);
        clickCatcher.SetDismissEnabled(false);
        clickCatcher.SetAllowedTarget(null);

        dimCanvas.gameObject.SetActive(true);
        inputCanvas.gameObject.SetActive(true);
        popupCanvas.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(DismissDelay);

        clickIndicator.SetActive(true);
        clickCatcher.SetDismissEnabled(true);
        yield return new WaitUntil(() => clickCatcher.ConsumeDismissRequest());

        clickIndicator.SetActive(false);
        popupCanvas.gameObject.SetActive(false);
        RestoreAllowedTargetParent(target);
        RemoveHighlight();
    }

    private void EndTutorial()
    {
        RemoveHighlight();
        if (dimCanvas != null)
            dimCanvas.gameObject.SetActive(false);
        if (inputCanvas != null)
            inputCanvas.gameObject.SetActive(false);
        if (popupCanvas != null)
        popupCanvas.gameObject.SetActive(false);
        tutorialActive = false;
        state = TutorialState.Idle;
        TryStartInventoryTutorial();
    }

    private void Highlight(RectTransform target)
    {
        RemoveHighlight();
        if (target == null)
            return;

        highlightedTarget = target;
        highlightCanvas = target.GetComponent<Canvas>();
        if (highlightCanvas == null)
        {
            highlightCanvas = target.gameObject.AddComponent<Canvas>();
            addedHighlightCanvas = true;
        }
        else
        {
            previousOverrideSorting = highlightCanvas.overrideSorting;
            previousSortingOrder = highlightCanvas.sortingOrder;
        }

        highlightCanvas.overrideSorting = true;
        highlightCanvas.sortingOrder = HighlightSortingOrder;

        // An override-sorting nested Canvas is a separate raycast surface. Some
        // scene variants already add a raycaster to tab buttons, while others
        // rely on the parent Canvas. Ensure the allowlisted target can receive
        // pointer events independently above the full-screen blocker.
        highlightRaycaster = target.GetComponent<GraphicRaycaster>();
        if (highlightRaycaster == null)
        {
            highlightRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();
            addedHighlightRaycaster = true;
        }
        else
        {
            previousRaycasterEnabled = highlightRaycaster.enabled;
        }

        highlightRaycaster.enabled = true;
    }

    private void RemoveHighlight()
    {
        if (highlightCanvas == null)
            return;

        if (addedHighlightRaycaster && highlightRaycaster != null)
            Destroy(highlightRaycaster);
        else if (highlightRaycaster != null)
            highlightRaycaster.enabled = previousRaycasterEnabled;

        if (addedHighlightCanvas)
            Destroy(highlightCanvas);
        else
        {
            highlightCanvas.overrideSorting = previousOverrideSorting;
            highlightCanvas.sortingOrder = previousSortingOrder;
        }

        highlightedTarget = null;
        highlightCanvas = null;
        highlightRaycaster = null;
        addedHighlightCanvas = false;
        addedHighlightRaycaster = false;
        previousRaycasterEnabled = false;
    }

    private void EnsureTutorialUI()
    {
        if (dimCanvas != null)
            return;

        dimCanvas = CreateCanvas("FTUE Dim Canvas", DimSortingOrder, true);
        Image dim = CreateImage("Screen Dimmer", dimCanvas.transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = false;

        inputCanvas = CreateCanvas("FTUE Input Blocker Canvas", InputSortingOrder, true);
        Image blocker = CreateImage("Tutorial Input Blocker", inputCanvas.transform, Color.clear);
        Stretch(blocker.rectTransform);
        blocker.raycastTarget = true;
        clickCatcher = blocker.gameObject.AddComponent<FTUEClickCatcher>();

        popupCanvas = CreateCanvas("FTUE Popup Canvas", PopupSortingOrder, false);
        popupRoot = new GameObject("Tutorial Popup", typeof(RectTransform), typeof(Image));
        popupRoot.transform.SetParent(popupCanvas.transform, false);
        popupRect = popupRoot.GetComponent<RectTransform>();
        popupRect.anchorMin = popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(720f, 390f);
        Image popupBackground = popupRoot.GetComponent<Image>();
        popupBackground.color = new Color(0.12f, 0.045f, 0.075f, 0.98f);
        popupBackground.raycastTarget = false;

        titleText = CreateText("Title", popupRoot.transform, 42f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.06f, 0.68f);
        titleRect.anchorMax = new Vector2(0.94f, 0.94f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        bodyText = CreateText("Body", popupRoot.transform, 29f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.08f, 0.23f);
        bodyRect.anchorMax = new Vector2(0.92f, 0.68f);
        bodyRect.offsetMin = bodyRect.offsetMax = Vector2.zero;

        clickIndicator = new GameObject("Left Click Indicator", typeof(RectTransform));
        clickIndicator.transform.SetParent(popupRoot.transform, false);
        RectTransform indicatorRect = clickIndicator.GetComponent<RectTransform>();
        indicatorRect.anchorMin = indicatorRect.anchorMax = new Vector2(0.5f, 0f);
        indicatorRect.pivot = new Vector2(0.5f, 0f);
        indicatorRect.anchoredPosition = new Vector2(0f, 28f);
        indicatorRect.sizeDelta = new Vector2(320f, 58f);
        TMP_Text indicator = CreateText("Label", clickIndicator.transform, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
        indicator.text = "LEFT CLICK TO CONTINUE";
        indicator.color = new Color(1f, 0.82f, 0.35f, 1f);
        Stretch(indicator.rectTransform);

        dimCanvas.gameObject.SetActive(false);
        inputCanvas.gameObject.SetActive(false);
        popupCanvas.gameObject.SetActive(false);
    }

    private void PositionPopupAwayFrom(RectTransform target)
    {
        float x = 0f;
        if (target != null)
        {
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera camera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
            x = screenPoint.x >= Screen.width * 0.5f ? -420f : 420f;
        }
        popupRect.anchoredPosition = new Vector2(x, 0f);
    }

    private static Canvas CreateCanvas(string objectName, int sortingOrder, bool raycaster)
    {
        GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(instance.transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (raycaster)
            canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.97f, 0.91f, 0.78f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = size;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public sealed class FTUEClickCatcher : MonoBehaviour, IPointerClickHandler, ICanvasRaycastFilter
{
    private bool dismissEnabled;
    private bool dismissRequested;
    private RectTransform allowedTarget;

    public void SetDismissEnabled(bool enabled)
    {
        dismissEnabled = enabled;
        dismissRequested = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dismissEnabled && eventData.button == PointerEventData.InputButton.Left)
            dismissRequested = true;
        else if (eventData.button == PointerEventData.InputButton.Left)
            FTUEManager.LogRaycastHits(eventData.position);
    }

    public void SetAllowedTarget(RectTransform target)
    {
        allowedTarget = target;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (allowedTarget == null || !allowedTarget.gameObject.activeInHierarchy)
            return true;

        Canvas targetCanvas = allowedTarget.GetComponentInParent<Canvas>();
        Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        // This is the single raycast hole in the global blocker. Unity can now
        // deliver the pointer to the real Potion Button and its existing action.
        return !RectTransformUtility.RectangleContainsScreenPoint(allowedTarget, screenPoint, targetCamera);
    }

    public bool ConsumeDismissRequest()
    {
        if (!dismissRequested)
            return false;
        dismissRequested = false;
        return true;
    }
}
