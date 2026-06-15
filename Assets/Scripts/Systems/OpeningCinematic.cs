using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OpeningCinematic : MonoBehaviour
{
    private const string TargetSceneName = "PrototypeScene";
    private const float IntroDuration = 13.8f;

    private static bool hasPlayedThisSession;

    private Camera mainCamera;
    private Vector3 gameplayCameraPosition;
    private float gameplayCameraSize;
    private Canvas overlayCanvas;
    private Image cinematicBackdrop;
    private RawImage cemeteryImage;
    private CanvasGroup cemeteryGroup;
    private Image fadeImage;
    private RectTransform topBar;
    private RectTransform bottomBar;
    private TextMeshProUGUI titleText;
    private CanvasGroup titleGroup;
    private RectTransform newspaperRect;
    private CanvasGroup newspaperGroup;
    private bool skipRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        hasPlayedThisSession = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartOpeningCinematic()
    {
        if (hasPlayedThisSession || SceneManager.GetActiveScene().name != TargetSceneName)
        {
            return;
        }

        hasPlayedThisSession = true;
        GameObject cinematicObject = new GameObject("Opening Cinematic");
        cinematicObject.AddComponent<OpeningCinematic>();
    }

    private IEnumerator Start()
    {
        yield return null;

        mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic)
        {
            Destroy(gameObject);
            yield break;
        }

        gameplayCameraPosition = mainCamera.transform.position;
        gameplayCameraSize = mainCamera.orthographicSize;

        BuildOverlay();
        ClearSelectedUI();

        mainCamera.transform.position = gameplayCameraPosition;
        mainCamera.orthographicSize = gameplayCameraSize;

        yield return PlayCinematic();
        FinishCinematic();
    }

    private IEnumerator PlayCinematic()
    {
        float elapsed = 0f;

        while (elapsed < IntroDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;

            AnimateFade(elapsed);
            AnimateBars(elapsed);
            AnimateCemetery(elapsed);
            AnimateCamera(elapsed);
            AnimateNewspaper(elapsed);
            AnimateTitle(elapsed);

            yield return null;
        }
    }

    private void AnimateFade(float elapsed)
    {
        float alpha;

        if (elapsed < 1.15f)
        {
            alpha = 1f - Smooth01(elapsed / 1.15f);
        }
        else if (elapsed < 9.75f)
        {
            alpha = 0f;
        }
        else if (elapsed < 10.35f)
        {
            alpha = Smooth01((elapsed - 9.75f) / 0.6f);
        }
        else if (elapsed < 11.05f)
        {
            alpha = 1f - Smooth01((elapsed - 10.35f) / 0.7f);
        }
        else
        {
            alpha = 0f;
        }

        SetImageAlpha(fadeImage, alpha);
    }

    private void AnimateBars(float elapsed)
    {
        float amount;

        if (elapsed < 1.3f)
        {
            amount = Smooth01(elapsed / 1.3f);
        }
        else if (elapsed > 12.85f)
        {
            amount = 1f - Smooth01((elapsed - 12.85f) / 0.75f);
        }
        else
        {
            amount = 1f;
        }

        float barHeight = Mathf.Lerp(0f, 72f, amount);
        topBar.sizeDelta = new Vector2(0f, barHeight);
        bottomBar.sizeDelta = new Vector2(0f, barHeight);
    }

    private void AnimateCamera(float elapsed)
    {
        mainCamera.transform.position = gameplayCameraPosition;
        mainCamera.orthographicSize = gameplayCameraSize;
    }

    private void AnimateCemetery(float elapsed)
    {
        if (cemeteryImage == null || cemeteryGroup == null)
        {
            return;
        }

        Rect tombView = new Rect(0.075f, 0.035f, 0.62f, 0.62f);
        Rect shopView = new Rect(0.37f, 0.39f, 0.58f, 0.58f);

        if (elapsed < 6.65f)
        {
            cemeteryImage.uvRect = tombView;
            cemeteryGroup.alpha = 1f;
            SetImageAlpha(cinematicBackdrop, 1f);
            return;
        }

        float shopProgress = Smooth01(Mathf.InverseLerp(6.65f, 9.55f, elapsed));
        cemeteryImage.uvRect = LerpRect(tombView, shopView, shopProgress);
        cemeteryGroup.alpha = elapsed < 10.35f ? 1f : 0f;
        SetImageAlpha(cinematicBackdrop, elapsed < 10.35f ? 1f : 0f);
    }

    private void AnimateNewspaper(float elapsed)
    {
        if (newspaperRect == null || newspaperGroup == null)
        {
            return;
        }

        if (elapsed < 1.2f)
        {
            newspaperGroup.alpha = 0f;
            return;
        }

        if (elapsed < 1.85f)
        {
            float progress = Smooth01((elapsed - 1.2f) / 0.65f);
            newspaperGroup.alpha = progress;
            newspaperRect.localScale = Vector3.one * Mathf.Lerp(0.76f, 1f, progress);
            newspaperRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-5f, -1f, progress));
            return;
        }

        if (elapsed < 6.15f)
        {
            newspaperGroup.alpha = 1f;
            newspaperRect.localScale = Vector3.one;
            newspaperRect.localRotation = Quaternion.Euler(0f, 0f, -1f);
            return;
        }

        float exitProgress = Smooth01((elapsed - 6.15f) / 0.5f);
        newspaperGroup.alpha = 1f - exitProgress;
        newspaperRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, exitProgress);
    }

    private void AnimateTitle(float elapsed)
    {
        float alpha;

        if (elapsed < 11.15f)
        {
            alpha = 0f;
        }
        else if (elapsed < 11.8f)
        {
            alpha = Smooth01((elapsed - 11.15f) / 0.65f);
        }
        else if (elapsed < 12.75f)
        {
            alpha = 1f;
        }
        else
        {
            alpha = 1f - Smooth01((elapsed - 12.75f) / 0.65f);
        }

        titleGroup.alpha = alpha;
        titleText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-12f, 14f, alpha));
    }

    private void FinishCinematic()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.position = gameplayCameraPosition;
            mainCamera.orthographicSize = gameplayCameraSize;
        }

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
        }

        Destroy(gameObject);
    }

    private void RequestSkip()
    {
        skipRequested = true;
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Opening Cinematic Overlay", typeof(RectTransform));
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        cinematicBackdrop = CreateImage("Cinematic Backdrop", canvasObject.transform, Color.black);
        StretchToParent(cinematicBackdrop.rectTransform);

        CreateCemeteryImage(canvasObject.transform);
        CreateNewspaper(canvasObject.transform);

        fadeImage = CreateImage("Fade", canvasObject.transform, Color.black);
        StretchToParent(fadeImage.rectTransform);
        Image inputBlocker = CreateImage("Input Blocker", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
        StretchToParent(inputBlocker.rectTransform);

        topBar = CreateBar("Top Cinematic Bar", canvasObject.transform, true);
        bottomBar = CreateBar("Bottom Cinematic Bar", canvasObject.transform, false);

        CreateTitle(canvasObject.transform);
        CreateSkipButton(canvasObject.transform);
    }

    private void CreateCemeteryImage(Transform parent)
    {
        Texture2D cemeteryTexture = Resources.Load<Texture2D>("Cinematics/OpeningCemetery");
        if (cemeteryTexture == null)
        {
            return;
        }

        GameObject cemeteryObject = new GameObject(
            "Cemetery",
            typeof(RectTransform),
            typeof(CanvasGroup));
        cemeteryObject.transform.SetParent(parent, false);

        cemeteryImage = cemeteryObject.AddComponent<RawImage>();
        cemeteryImage.texture = cemeteryTexture;
        cemeteryImage.color = Color.white;
        cemeteryImage.raycastTarget = false;

        RectTransform rect = cemeteryImage.rectTransform;
        StretchToParent(rect);
        cemeteryImage.uvRect = new Rect(0.075f, 0.035f, 0.62f, 0.62f);

        cemeteryGroup = cemeteryObject.GetComponent<CanvasGroup>();
        cemeteryGroup.alpha = 1f;
        cemeteryGroup.blocksRaycasts = false;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(CanvasGroup));
        titleObject.transform.SetParent(parent, false);

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1100f, 220f);

        titleGroup = titleObject.GetComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleGroup.blocksRaycasts = false;

        titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "VOODOO STORE";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 96f;
        titleText.fontStyle = FontStyles.SmallCaps;
        titleText.color = new Color(0.96f, 0.82f, 0.43f, 1f);
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 42f;
        titleText.fontSizeMax = 96f;
        titleText.outlineWidth = 0.18f;
        titleText.outlineColor = new Color32(31, 12, 35, 230);
        titleText.raycastTarget = false;
    }

    private void CreateNewspaper(Transform parent)
    {
        GameObject newspaperObject = new GameObject(
            "NecroUltimate Newspaper",
            typeof(RectTransform),
            typeof(CanvasGroup));
        newspaperObject.transform.SetParent(parent, false);

        newspaperRect = newspaperObject.GetComponent<RectTransform>();
        newspaperRect.anchorMin = new Vector2(0.5f, 0.5f);
        newspaperRect.anchorMax = new Vector2(0.5f, 0.5f);
        newspaperRect.pivot = new Vector2(0.5f, 0.5f);
        newspaperRect.anchoredPosition = new Vector2(500f, -20f);
        newspaperRect.sizeDelta = new Vector2(700f, 780f);
        newspaperRect.localScale = Vector3.one * 0.76f;
        newspaperRect.localRotation = Quaternion.Euler(0f, 0f, -5f);

        newspaperGroup = newspaperObject.GetComponent<CanvasGroup>();
        newspaperGroup.alpha = 0f;
        newspaperGroup.blocksRaycasts = false;

        Image paper = newspaperObject.AddComponent<Image>();
        paper.color = new Color32(222, 204, 164, 255);
        paper.raycastTarget = false;

        Shadow shadow = newspaperObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(20f, -20f);

        TextMeshProUGUI masthead = CreateNewspaperText(
            "Masthead",
            newspaperObject.transform,
            "THE VOODOO GAZETTE",
            50f,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetNewspaperRect(masthead.rectTransform, new Vector2(0f, 348f), new Vector2(680f, 74f));

        Image topRule = CreateImage("Top Rule", newspaperObject.transform, new Color32(55, 43, 32, 255));
        SetNewspaperRect(topRule.rectTransform, new Vector2(0f, 302f), new Vector2(650f, 5f));

        TextMeshProUGUI headline = CreateNewspaperText(
            "Headline",
            newspaperObject.transform,
            "NECROULTIMATE",
            56f,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetNewspaperRect(headline.rectTransform, new Vector2(0f, 246f), new Vector2(680f, 86f));

        Texture2D potionTexture = Resources.Load<Texture2D>("Cinematics/NecroUltimatePotion");
        if (potionTexture != null)
        {
            GameObject potionObject = new GameObject("Potion Illustration", typeof(RectTransform));
            potionObject.transform.SetParent(newspaperObject.transform, false);
            RawImage potionImage = potionObject.AddComponent<RawImage>();
            potionImage.texture = potionTexture;
            potionImage.color = Color.white;
            potionImage.raycastTarget = false;
            SetNewspaperRect(potionImage.rectTransform, new Vector2(0f, 5f), new Vector2(390f, 430f));
        }

        Image bottomRule = CreateImage("Bottom Rule", newspaperObject.transform, new Color32(55, 43, 32, 255));
        SetNewspaperRect(bottomRule.rectTransform, new Vector2(0f, -236f), new Vector2(650f, 4f));

        TextMeshProUGUI body = CreateNewspaperText(
            "Story",
            newspaperObject.transform,
            "A forbidden potion with an impossible promise:\nIT CAN BRING PEOPLE BACK FROM THE DEAD.",
            34f,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        body.enableAutoSizing = true;
        body.fontSizeMin = 24f;
        body.fontSizeMax = 34f;
        body.lineSpacing = 5f;
        SetNewspaperRect(body.rectTransform, new Vector2(0f, -312f), new Vector2(640f, 130f));
    }

    private static TextMeshProUGUI CreateNewspaperText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = new Color32(48, 36, 28, 255);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static void SetNewspaperRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void CreateSkipButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Skip Button", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-42f, 34f);
        rect.sizeDelta = new Vector2(150f, 52f);

        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.025f, 0.055f, 0.78f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(RequestSkip);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchToParent(labelRect);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "SKIP";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 25f;
        label.color = new Color(0.96f, 0.88f, 0.68f, 1f);
        label.raycastTarget = false;
    }

    private static RectTransform CreateBar(string name, Transform parent, bool top)
    {
        Image image = CreateImage(name, parent, Color.black);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = top ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.anchorMax = top ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 0f);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ClearSelectedUI()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static Rect LerpRect(Rect from, Rect to, float progress)
    {
        return new Rect(
            Mathf.Lerp(from.x, to.x, progress),
            Mathf.Lerp(from.y, to.y, progress),
            Mathf.Lerp(from.width, to.width, progress),
            Mathf.Lerp(from.height, to.height, progress));
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void OnDestroy()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.position = gameplayCameraPosition;
            mainCamera.orthographicSize = gameplayCameraSize;
        }
    }
}
