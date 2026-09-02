using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SellConfirmPanelVisual : MonoBehaviour
{
    private const string ShellResource = "SellConfirmPanelFortuneScaleShell";
    private static Sprite softParticleSprite;

    [Header("Panel Layout (normalized anchors)")]
    [SerializeField] private Vector2 panelAnchorMin = new Vector2(0.025f, 0.035f);
    [SerializeField] private Vector2 panelAnchorMax = new Vector2(0.975f, 0.965f);
    [SerializeField] private Vector2 offerAreaMin = new Vector2(0.355f, 0.085f);
    [SerializeField] private Vector2 offerAreaMax = new Vector2(0.795f, 0.595f);
    [SerializeField, Range(0.1f, 0.3f)] private float offerRowHeight = 0.22f;
    [SerializeField, Range(0.15f, 0.35f)] private float offerRowStep = 0.25f;
    [SerializeField] private Vector2 itemIconMin = new Vector2(0.405f, 0.62f);
    [SerializeField] private Vector2 itemIconMax = new Vector2(0.59f, 0.84f);
    [SerializeField] private Vector2 itemNameMin = new Vector2(0.59f, 0.655f);
    [SerializeField] private Vector2 itemNameMax = new Vector2(0.765f, 0.79f);
    [SerializeField] private Vector2 closeButtonMin = new Vector2(0.735f, 0.83f);
    [SerializeField] private Vector2 closeButtonMax = new Vector2(0.815f, 0.96f);
    [SerializeField, Min(1f)] private float itemNameFontMin = 16f;
    [SerializeField, Min(1f)] private float itemNameFontMax = 34f;
    [SerializeField, Min(1f)] private float offerFontMin = 14f;
    [SerializeField, Min(1f)] private float offerFontMax = 30f;

    [Header("Editable Colors")]
    [SerializeField] private Color backdropColor = new Color(0.015f, 0.008f, 0.01f, 0.72f);
    [SerializeField] private Color safeColor = new Color32(111, 175, 122, 255);
    [SerializeField] private Color fairColor = new Color32(199, 160, 78, 255);
    [SerializeField] private Color riskyColor = new Color32(199, 106, 87, 255);
    [SerializeField] private Color temptFateColor = new Color32(44, 222, 202, 255);
    [SerializeField] private Color normalTextColor = new Color(0.16f, 0.075f, 0.035f, 1f);
    [SerializeField] private Color hoverTextColor = new Color(1f, 0.91f, 0.72f, 1f);
    [SerializeField] private Color itemNameColor = new Color(0.13f, 0.055f, 0.025f, 1f);

    [Header("Button Feel")]
    [SerializeField, Range(0f, 1f)] private float hoverGlowAlpha = 0.26f;
    [SerializeField, Range(0f, 0.2f)] private float temptFateIdlePulse = 0.07f;

    [Header("Canvas Sorting")]
    [SerializeField] private bool overrideSorting = true;
    [SerializeField] private int sortingOrder = 200;

    [Header("Magical Wisps")]
    [SerializeField] private Color wispColor = new Color(0.16f, 0.95f, 0.83f, 0.55f);
    [SerializeField, Range(0f, 30f)] private float wispRiseSpeed = 11f;

    private RectTransform visualRoot;
    private RectTransform choicesRoot;
    private Image itemIcon;
    private TMP_Text itemName;
    private CanvasGroup canvasGroup;
    private readonly List<RectTransform> wisps = new List<RectTransform>();
    private readonly List<float> wispPhases = new List<float>();
    private bool opening;
    private float openTime;

    // Authoring is explicit and one-time. Nothing in this component changes
    // RectTransforms when Unity validates or saves the scene.
    public void Build(RectTransform offerChoicesRoot, IReadOnlyList<Button> buttons, Action closeAction)
    {
        CacheExistingVisuals();
        bool preserveAuthoredLayout = visualRoot != null;
        choicesRoot = offerChoicesRoot;
        EnsureShell();
        ConfigureButtons(buttons, preserveAuthoredLayout);
        EnsureCloseButton(closeAction);
        EnsureWisps();
    }

    public void BindRuntime(Action closeAction)
    {
        CacheExistingVisuals();
        if (visualRoot == null) return;
        Transform closeTransform = visualRoot.Find("Close Button Hit Area");
        Button close = closeTransform != null ? closeTransform.GetComponent<Button>() : null;
        if (close != null)
        {
            close.interactable = true;
            Image closeImage = close.GetComponent<Image>();
            if (closeImage != null) closeImage.raycastTarget = true;
            close.onClick.RemoveAllListeners();
            close.onClick.AddListener(() => closeAction?.Invoke());
            close.transform.SetAsLastSibling();
        }
        CacheExistingWisps();
    }

    public void ApplySorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;
        canvas.overrideSorting = overrideSorting;
        if (overrideSorting) canvas.sortingOrder = sortingOrder;
    }

    public void SetItem(string displayName, Sprite icon)
    {
        CacheExistingVisuals();
        if (itemName == null || itemIcon == null) return;
        itemName.text = string.IsNullOrWhiteSpace(displayName) ? "UNKNOWN ITEM" : displayName.ToUpperInvariant();
        itemIcon.sprite = icon;
        itemIcon.enabled = icon != null;
    }

    public void PlayOpen()
    {
        CacheExistingVisuals();
        if (visualRoot == null || canvasGroup == null) return;
        opening = true;
        openTime = 0f;
        canvasGroup.alpha = 0f;
        visualRoot.localScale = Vector3.one * 0.92f;
    }

    [ContextMenu("Show Editable Preview")]
    public void ShowEditablePreview()
    {
        gameObject.SetActive(true);
    }

    [ContextMenu("Hide Editable Preview")]
    public void HideEditablePreview()
    {
        if (!Application.isPlaying) gameObject.SetActive(false);
    }

    private void Update()
    {
        if (opening)
        {
            openTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(openTime / 0.22f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = eased;
            visualRoot.localScale = Vector3.LerpUnclamped(Vector3.one * 0.92f, Vector3.one, eased);
            opening = t < 1f;
        }

        if (!Application.isPlaying) return;

        for (int i = 0; i < wisps.Count; i++)
        {
            RectTransform wisp = wisps[i];
            float phase = wispPhases[i];
            Vector2 p = wisp.anchoredPosition;
            p.y += Time.unscaledDeltaTime * (wispRiseSpeed + i % 4 * 2f);
            p.x += Mathf.Sin(Time.unscaledTime * 1.7f + phase) * Time.unscaledDeltaTime * 4f;
            if (p.y > 18f) p.y = -22f;
            wisp.anchoredPosition = p;
            float pulse = 0.35f + 0.35f * (Mathf.Sin(Time.unscaledTime * 2.3f + phase) + 1f) * 0.5f;
            wisp.GetComponent<Image>().color = new Color(wispColor.r, wispColor.g, wispColor.b, pulse * wispColor.a);
        }
    }

    private void EnsureShell()
    {
        CacheExistingVisuals();
        if (visualRoot != null)
        {
            return;
        }

        RectTransform panelRect = transform as RectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        foreach (Transform child in transform)
        {
            if (child.name != "Sell Offer Choices")
                child.gameObject.SetActive(false);
        }

        Image oldBackground = GetComponent<Image>();
        if (oldBackground != null)
        {
            oldBackground.sprite = null;
            oldBackground.color = Color.clear;
            oldBackground.raycastTarget = true;
        }

        GameObject rootObject = new GameObject("Fortune Scale Visual Root", typeof(RectTransform), typeof(CanvasGroup));
        visualRoot = rootObject.GetComponent<RectTransform>();
        visualRoot.SetParent(transform, false);
        visualRoot.anchorMin = panelAnchorMin;
        visualRoot.anchorMax = panelAnchorMax;
        visualRoot.offsetMin = visualRoot.offsetMax = Vector2.zero;
        canvasGroup = rootObject.GetComponent<CanvasGroup>();

        Image shell = rootObject.AddComponent<Image>();
        shell.sprite = Resources.Load<Sprite>(ShellResource);
        if (shell.sprite == null)
        {
            Texture2D shellTexture = Resources.Load<Texture2D>(ShellResource);
            if (shellTexture != null)
                shell.sprite = Sprite.Create(shellTexture, new Rect(0f, 0f, shellTexture.width, shellTexture.height), Vector2.one * 0.5f, 100f);
        }
        shell.preserveAspect = false;
        shell.raycastTarget = false;

        itemIcon = CreateImage("Live Item Icon", visualRoot, itemIconMin, itemIconMax);
        itemIcon.preserveAspect = true;
        itemIcon.raycastTarget = false;

        itemName = CreateText("Live Item Name", visualRoot, itemNameMin, itemNameMax);
        itemName.alignment = TextAlignmentOptions.Center;
        itemName.enableAutoSizing = true;
        itemName.fontSizeMin = itemNameFontMin;
        itemName.fontSizeMax = itemNameFontMax;
        itemName.fontStyle = FontStyles.Bold;
        itemName.color = itemNameColor;
    }

    private void ConfigureButtons(IReadOnlyList<Button> buttons, bool preserveAuthoredLayout)
    {
        if (choicesRoot == null || buttons == null) return;
        if (!preserveAuthoredLayout)
        {
            choicesRoot.SetParent(visualRoot, false);
            choicesRoot.anchorMin = offerAreaMin;
            choicesRoot.anchorMax = offerAreaMax;
            choicesRoot.offsetMin = choicesRoot.offsetMax = Vector2.zero;
        }

        Color[] accents = { safeColor, fairColor, riskyColor, temptFateColor };

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            RectTransform rect = button.transform as RectTransform;
            if (!preserveAuthoredLayout)
            {
                float top = 1f - i * offerRowStep;
                rect.anchorMin = new Vector2(0f, top - offerRowHeight);
                rect.anchorMax = new Vector2(1f, top);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            Image image = button.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(accents[i].r, accents[i].g, accents[i].b, 0.015f);
            button.transition = Selectable.Transition.None;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (!preserveAuthoredLayout)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10f, 1f);
                labelRect.offsetMax = new Vector2(-10f, -1f);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = offerFontMin;
                label.fontSizeMax = offerFontMax;
                label.fontStyle = FontStyles.Normal;
                label.color = normalTextColor;
                label.raycastTarget = false;
            }

            SellOfferButtonVFX vfx = button.GetComponent<SellOfferButtonVFX>();
            if (vfx == null) vfx = button.gameObject.AddComponent<SellOfferButtonVFX>();
            vfx.Configure(accents[i], i == 3, normalTextColor, hoverTextColor, hoverGlowAlpha, temptFateIdlePulse);
        }
    }

    private void CacheExistingVisuals()
    {
        if (visualRoot == null)
        {
            Transform existing = transform.Find("Fortune Scale Visual Root");
            if (existing != null) visualRoot = existing as RectTransform;
        }
        if (visualRoot == null) return;
        if (canvasGroup == null) canvasGroup = visualRoot.GetComponent<CanvasGroup>();
        if (itemIcon == null)
        {
            Transform existing = visualRoot.Find("Live Item Icon");
            if (existing != null) itemIcon = existing.GetComponent<Image>();
        }
        if (itemName == null)
        {
            Transform existing = visualRoot.Find("Live Item Name");
            if (existing != null) itemName = existing.GetComponent<TMP_Text>();
        }
        if (choicesRoot == null)
        {
            Transform existing = visualRoot.Find("Sell Offer Choices");
            if (existing != null) choicesRoot = existing as RectTransform;
        }
    }

    private void EnsureCloseButton(Action closeAction)
    {
        Transform old = visualRoot.Find("Close Button Hit Area");
        Button close = old != null ? old.GetComponent<Button>() : null;
        if (close == null)
        {
            GameObject closeObject = new GameObject("Close Button Hit Area", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = closeObject.GetComponent<RectTransform>();
            rect.SetParent(visualRoot, false);
            rect.anchorMin = closeButtonMin;
            rect.anchorMax = closeButtonMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = closeObject.GetComponent<Image>();
            image.color = new Color(0.55f, 0.16f, 0.08f, 0.01f);
            close = closeObject.GetComponent<Button>();
            close.targetGraphic = image;
        }
        close.onClick.RemoveAllListeners();
        close.onClick.AddListener(() => closeAction?.Invoke());
        RectTransform closeRect = close.transform as RectTransform;
        closeRect.anchorMin = closeButtonMin;
        closeRect.anchorMax = closeButtonMax;
        closeRect.offsetMin = closeRect.offsetMax = Vector2.zero;
    }

    private void EnsureWisps()
    {
        if (wisps.Count > 0) return;
        Sprite particle = GetSoftParticleSprite();
        for (int i = 0; i < 11; i++)
        {
            Image image = CreateImage("Tempt Fate Wisp " + (i + 1), visualRoot,
                new Vector2(0.31f + i * 0.045f, 0.03f), new Vector2(0.32f + i * 0.045f, 0.05f));
            image.sprite = particle;
            image.color = wispColor;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            float size = 5f + i % 4 * 2f;
            rect.sizeDelta = new Vector2(size, size);
            wisps.Add(rect);
            wispPhases.Add(i * 0.73f);
        }
    }

    private void CacheExistingWisps()
    {
        if (visualRoot == null || wisps.Count > 0) return;
        for (int i = 0; i < visualRoot.childCount; i++)
        {
            RectTransform child = visualRoot.GetChild(i) as RectTransform;
            if (child == null || !child.name.StartsWith("Tempt Fate Wisp", StringComparison.Ordinal)) continue;
            wisps.Add(child);
            wispPhases.Add(wisps.Count * 0.73f);
        }
    }

    private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return obj.GetComponent<Image>();
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return obj.GetComponent<TextMeshProUGUI>();
    }

    private static Sprite GetSoftParticleSprite()
    {
        if (softParticleSprite != null) return softParticleSprite;
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Sell Offer Soft Wisp" };
        Color[] pixels = new Color[size * size];
        Vector2 center = Vector2.one * (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
            float alpha = Mathf.Clamp01(1f - distance);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        softParticleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f);
        return softParticleSprite;
    }
}
