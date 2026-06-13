using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PageFoldScreenTransition : MonoBehaviour
{
    public enum TransitionStyle
    {
        CenterIris,
        PageFold
    }

    public enum FoldDirection
    {
        RightToLeft,
        LeftToRight
    }

    [Header("Screen Switching")]
    [SerializeField] private List<GameObject> screensToClose = new List<GameObject>();
    [SerializeField] private bool closeScreensBeforeOpening = true;

    [Header("Timing")]
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float holdTime = 0.05f;

    [Header("Look")]
    [SerializeField] private TransitionStyle transitionStyle = TransitionStyle.CenterIris;
    [SerializeField] private FoldDirection foldDirection = FoldDirection.RightToLeft;
    [SerializeField] private Color pageColor = new Color(0.98f, 0.91f, 0.72f, 1f);
    [SerializeField] private Color foldShadowColor = new Color(0.12f, 0.08f, 0.04f, 0.65f);
    [SerializeField] private Color backgroundFadeColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent actionsAfterClose;
    [SerializeField] private UnityEvent onFoldClosed;
    [SerializeField] private UnityEvent onTransitionComplete;

    private Canvas transitionCanvas;
    private Canvas rootCanvas;
    private CanvasGroup overlayGroup;
    private RectTransform overlayRoot;
    private RectTransform page;
    private Image pageImage;
    private Image shadowImage;
    private Image edgeImage;
    private RectTransform iris;
    private Image irisImage;
    private Image dimImage;
    private Coroutine activeTransition;

    public void TransitionToScreen(GameObject screenToOpen)
    {
        BeginTransition(() =>
        {
            if (closeScreensBeforeOpening)
            {
                CloseConfiguredScreens();
            }

            if (screenToOpen != null)
            {
                screenToOpen.SetActive(true);
            }
        });
    }

    public void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("PageFoldScreenTransition cannot load an empty scene name.", this);
            return;
        }

        BeginTransition(() => SceneManager.LoadScene(sceneName));
    }

    public void Play(UnityEvent actionAtFoldClosed)
    {
        BeginTransition(() => actionAtFoldClosed?.Invoke());
    }

    public void PlayOnly()
    {
        BeginTransition(null);
    }

    public void PlayFadeEffect()
    {
        BeginTransition(null);
    }

    public void PlayFadeEffectThenRunActions()
    {
        BeginTransition(() => actionsAfterClose?.Invoke());
    }

    public void CloseConfiguredScreens()
    {
        for (int i = 0; i < screensToClose.Count; i++)
        {
            if (screensToClose[i] != null)
            {
                screensToClose[i].SetActive(false);
            }
        }
    }

    public float TotalDuration
    {
        get { return closeDuration + holdTime + openDuration; }
    }

    public float CoveredDelay
    {
        get { return closeDuration; }
    }

    private void BeginTransition(System.Action actionAtFoldClosed)
    {
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }

        EnsureOverlay();
        if (overlayRoot == null)
        {
            return;
        }

        activeTransition = StartCoroutine(TransitionRoutine(actionAtFoldClosed));
    }

    private IEnumerator TransitionRoutine(System.Action actionAtFoldClosed)
    {
        overlayRoot.gameObject.SetActive(true);
        overlayGroup.alpha = 1f;
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = true;

        yield return AnimatePage(0f, 1f, closeDuration);

        actionAtFoldClosed?.Invoke();
        onFoldClosed?.Invoke();

        if (holdTime > 0f)
        {
            yield return new WaitForSecondsRealtime(holdTime);
        }

        yield return AnimatePage(1f, 0f, openDuration);

        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;
        overlayRoot.gameObject.SetActive(false);
        activeTransition = null;
        onTransitionComplete?.Invoke();
    }

    private IEnumerator AnimatePage(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float rawT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float t = ease != null ? ease.Evaluate(rawT) : rawT;
            SetTransition(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTransition(to);
    }

    private void SetTransition(float amount)
    {
        if (transitionStyle == TransitionStyle.CenterIris)
        {
            SetIris(amount);
            return;
        }

        SetFold(amount);
    }

    private void SetIris(float amount)
    {
        amount = Mathf.Clamp01(amount);

        page.gameObject.SetActive(false);
        iris.gameObject.SetActive(true);
        iris.SetAsLastSibling();

        Rect rect = overlayRoot.rect;
        float width = rect.width > 0f ? rect.width : Screen.width;
        float height = rect.height > 0f ? rect.height : Screen.height;
        float maxSize = Mathf.Sqrt(width * width + height * height) * 1.2f;
        float minSize = 12f;
        float size = Mathf.Lerp(minSize, maxSize, amount);

        iris.anchorMin = new Vector2(0.5f, 0.5f);
        iris.anchorMax = new Vector2(0.5f, 0.5f);
        iris.pivot = new Vector2(0.5f, 0.5f);
        iris.anchoredPosition = Vector2.zero;
        iris.sizeDelta = new Vector2(size, size);

        Color color = pageColor;
        color.a = amount <= 0.001f ? 0f : pageColor.a;
        irisImage.color = color;

        Color dimColor = backgroundFadeColor;
        dimColor.a = backgroundFadeColor.a * amount;
        dimImage.color = dimColor;
    }

    private void SetFold(float amount)
    {
        amount = Mathf.Clamp01(amount);

        iris.gameObject.SetActive(false);
        page.gameObject.SetActive(true);

        float direction = foldDirection == FoldDirection.RightToLeft ? -1f : 1f;
        float rotation = Mathf.Lerp(88f, 0f, amount) * direction;
        float pageX = foldDirection == FoldDirection.RightToLeft ? 1f : 0f;
        float canvasWidth = overlayRoot.rect.width > 0f ? overlayRoot.rect.width : 1920f;
        float anchoredX = Mathf.Lerp(direction * canvasWidth * 0.5f, 0f, amount);
        float edgeX = foldDirection == FoldDirection.RightToLeft ? 0f : 1f;

        page.pivot = new Vector2(pageX, 0.5f);
        page.anchorMin = new Vector2(pageX, 0f);
        page.anchorMax = new Vector2(pageX, 1f);
        page.sizeDelta = new Vector2(canvasWidth, 0f);
        page.anchoredPosition = new Vector2(anchoredX, 0f);
        page.localRotation = Quaternion.Euler(0f, rotation, 0f);

        edgeImage.rectTransform.anchorMin = new Vector2(edgeX, 0f);
        edgeImage.rectTransform.anchorMax = new Vector2(edgeX, 1f);
        edgeImage.rectTransform.pivot = new Vector2(edgeX, 0.5f);

        float shadowAlpha = Mathf.Sin(amount * Mathf.PI) * foldShadowColor.a;
        Color shadowColor = foldShadowColor;
        shadowColor.a = shadowAlpha;
        shadowImage.color = shadowColor;

        Color edgeColor = foldShadowColor;
        edgeColor.a = Mathf.Lerp(0.05f, 0.35f, amount);
        edgeImage.color = edgeColor;

        Color dimColor = backgroundFadeColor;
        dimColor.a = backgroundFadeColor.a * amount;
        dimImage.color = dimColor;
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Page Fold Transition Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        transitionCanvas = canvasObject.GetComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = 32767;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject = new GameObject("Page Fold Transition Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        overlayObject.transform.SetAsLastSibling();

        overlayRoot = overlayObject.GetComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        overlayGroup = overlayObject.GetComponent<CanvasGroup>();

        dimImage = overlayObject.GetComponent<Image>();
        dimImage.raycastTarget = true;

        page = CreateImage("Folding Page", overlayRoot, pageColor).rectTransform;
        page.anchorMin = new Vector2(1f, 0f);
        page.anchorMax = new Vector2(1f, 1f);
        page.sizeDelta = new Vector2(1920f, 0f);
        pageImage = page.GetComponent<Image>();
        pageImage.raycastTarget = false;

        shadowImage = CreateImage("Page Fold Shadow", page, foldShadowColor);
        Stretch(shadowImage.rectTransform);

        edgeImage = CreateImage("Page Edge", page, foldShadowColor);
        RectTransform edge = edgeImage.rectTransform;
        edge.anchorMin = new Vector2(0f, 0f);
        edge.anchorMax = new Vector2(0f, 1f);
        edge.pivot = new Vector2(0f, 0.5f);
        edge.sizeDelta = new Vector2(18f, 0f);
        edge.anchoredPosition = Vector2.zero;

        iris = CreateImage("Center Iris Fade", overlayRoot, pageColor).rectTransform;
        irisImage = iris.GetComponent<Image>();
        irisImage.sprite = CreateIrisSprite();
        irisImage.type = Image.Type.Simple;
        irisImage.preserveAspect = true;
        irisImage.raycastTarget = false;

        overlayObject.SetActive(false);
    }

    private Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Sprite CreateIrisSprite()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        float softEdge = size * 0.08f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / softEdge);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
