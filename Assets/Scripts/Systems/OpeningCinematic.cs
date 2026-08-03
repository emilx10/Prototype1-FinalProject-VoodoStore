using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OpeningCinematic : MonoBehaviour
{
    private const string TargetSceneName = "PrototypeScene";
    private const float IntroHoldDuration = 8.2f;
    private const float TransitionDuration = 4.2f;

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
    private RectTransform clickPromptRect;
    private CanvasGroup clickPromptGroup;
    private bool skipRequested;
    private bool musicHandedOff;

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
            musicHandedOff = true;
            AudioManager.Instance?.PlayGameplayMusicImmediately();
            Destroy(gameObject);
            yield break;
        }

        AudioManager.Instance?.PlayOpeningMusic();

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

        while (elapsed < IntroHoldDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;

            AnimateIntroFade(elapsed);
            AnimateBars(elapsed);
            AnimateCemetery(elapsed);
            AnimateCamera(elapsed);
            AnimateNewspaper(elapsed);
            AnimateTitle(elapsed);
            AnimateClickPrompt(elapsed, false);

            yield return null;
        }

        while (!skipRequested && !Input.GetMouseButtonDown(0))
        {
            AnimateIntroFade(IntroHoldDuration);
            AnimateBars(IntroHoldDuration);
            AnimateCemetery(IntroHoldDuration);
            AnimateCamera(IntroHoldDuration);
            AnimateNewspaper(IntroHoldDuration);
            AnimateTitle(IntroHoldDuration);
            AnimateClickPrompt(IntroHoldDuration, true);

            yield return null;
        }

        musicHandedOff = true;
        AudioManager.Instance?.CrossfadeToGameplayMusic(TransitionDuration);
        ClearSelectedUI();
        float transitionElapsed = 0f;
        while (transitionElapsed < TransitionDuration)
        {
            transitionElapsed += Time.unscaledDeltaTime;

            AnimateTransition(transitionElapsed);
            yield return null;
        }
    }

    private void AnimateIntroFade(float elapsed)
    {
        float alpha = elapsed < 1.15f ? 1f - Smooth01(elapsed / 1.15f) : 0f;
        SetImageAlpha(fadeImage, alpha);
    }

    private void AnimateBars(float elapsed)
    {
        float amount;

        if (elapsed < 1.3f)
        {
            amount = Smooth01(elapsed / 1.3f);
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

        Rect houseView = new Rect(0.58f, 0.18f, 0.38f, 0.38f);
        Rect fullView = new Rect(0f, 0f, 1f, 1f);

        if (elapsed < 1.2f)
        {
            cemeteryImage.uvRect = houseView;
            cemeteryGroup.alpha = 1f;
            SetImageAlpha(cinematicBackdrop, 1f);
            return;
        }

        float graveProgress = Smooth01(Mathf.InverseLerp(1.2f, 3.9f, elapsed));
        cemeteryImage.uvRect = LerpRect(houseView, fullView, graveProgress);
        cemeteryGroup.alpha = 1f;
        SetImageAlpha(cinematicBackdrop, 1f);
    }

    private void AnimateNewspaper(float elapsed)
    {
        if (newspaperRect == null || newspaperGroup == null)
        {
            return;
        }

        if (elapsed < 3.95f)
        {
            newspaperGroup.alpha = 0f;
            return;
        }

        if (elapsed < 4.65f)
        {
            float progress = Smooth01((elapsed - 3.95f) / 0.7f);
            newspaperGroup.alpha = progress;
            newspaperRect.localScale = Vector3.one * Mathf.Lerp(0.76f, 1f, progress);
            newspaperRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-5f, -1f, progress));
            return;
        }

        newspaperGroup.alpha = 1f;
        newspaperRect.localScale = Vector3.one;
        newspaperRect.localRotation = Quaternion.Euler(0f, 0f, -1f);
    }

    private void AnimateTitle(float elapsed)
    {
        titleGroup.alpha = 0f;
    }

    private void AnimateClickPrompt(float elapsed, bool waitingForClick)
    {
        if (clickPromptRect == null || clickPromptGroup == null)
            return;

        float introAlpha = elapsed < 6.4f ? 0f : Smooth01(Mathf.InverseLerp(6.4f, 7.1f, elapsed));
        clickPromptGroup.alpha = waitingForClick ? 0.74f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.18f : introAlpha;
        clickPromptRect.localScale = Vector3.one * (waitingForClick ? 1f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.035f : 1f);
    }

    private void AnimateTransition(float elapsed)
    {
        float fadeAlpha;
        if (elapsed < 0.65f)
            fadeAlpha = Smooth01(elapsed / 0.65f);
        else if (elapsed < 1.25f)
            fadeAlpha = 1f;
        else if (elapsed < 2f)
            fadeAlpha = 1f - Smooth01((elapsed - 1.25f) / 0.75f);
        else
            fadeAlpha = 0f;

        SetImageAlpha(fadeImage, fadeAlpha);

        float hideIntro = Smooth01(Mathf.InverseLerp(0f, 0.55f, elapsed));
        if (cemeteryGroup != null)
            cemeteryGroup.alpha = 1f - hideIntro;
        if (newspaperGroup != null)
            newspaperGroup.alpha = 1f - hideIntro;
        if (clickPromptGroup != null)
            clickPromptGroup.alpha = 0f;
        SetImageAlpha(cinematicBackdrop, 1f - hideIntro);

        float barAmount = elapsed < 0.65f
            ? 1f
            : 1f - Smooth01(Mathf.InverseLerp(3.25f, 4f, elapsed));
        float barHeight = Mathf.Lerp(0f, 72f, barAmount);
        topBar.sizeDelta = new Vector2(0f, barHeight);
        bottomBar.sizeDelta = new Vector2(0f, barHeight);

        float titleAlpha;
        if (elapsed < 1.2f)
            titleAlpha = 0f;
        else if (elapsed < 1.9f)
            titleAlpha = Smooth01((elapsed - 1.2f) / 0.7f);
        else if (elapsed < 3.2f)
            titleAlpha = 1f;
        else
            titleAlpha = 1f - Smooth01((elapsed - 3.2f) / 0.65f);

        titleGroup.alpha = titleAlpha;
        titleText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-12f, 14f, titleAlpha));

        if (mainCamera != null)
        {
            mainCamera.transform.position = gameplayCameraPosition;
            mainCamera.orthographicSize = gameplayCameraSize;
        }
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
        CreateClickPrompt(canvasObject.transform);

        fadeImage = CreateImage("Fade", canvasObject.transform, Color.black);
        StretchToParent(fadeImage.rectTransform);
        Image inputBlocker = CreateImage("Input Blocker", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
        StretchToParent(inputBlocker.rectTransform);

        topBar = CreateBar("Top Cinematic Bar", canvasObject.transform, true);
        bottomBar = CreateBar("Bottom Cinematic Bar", canvasObject.transform, false);

        CreateTitle(canvasObject.transform);
    }

    private void CreateCemeteryImage(Transform parent)
    {
        Texture2D cemeteryTexture = Resources.Load<Texture2D>("Cinematics/OpeningVivianFlamel");
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
        cemeteryImage.uvRect = new Rect(0.58f, 0.18f, 0.38f, 0.38f);

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
        titleText.text = "VOODO STORE";
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
        newspaperRect.anchoredPosition = new Vector2(565f, -18f);
        newspaperRect.sizeDelta = new Vector2(660f, 770f);
        newspaperRect.localScale = Vector3.one * 0.76f;
        newspaperRect.localRotation = Quaternion.Euler(0f, 0f, -5f);

        newspaperGroup = newspaperObject.GetComponent<CanvasGroup>();
        newspaperGroup.alpha = 0f;
        newspaperGroup.blocksRaycasts = false;

        Image paper = newspaperObject.AddComponent<Image>();
        paper.sprite = CreateNewspaperPaperSprite(660, 770);
        paper.type = Image.Type.Simple;
        paper.color = Color.white;
        paper.raycastTarget = false;

        Shadow shadow = newspaperObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(20f, -20f);

        TextMeshProUGUI masthead = CreateNewspaperText(
            "Masthead",
            newspaperObject.transform,
            "THE VOODOO GAZETTE",
            46f,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetNewspaperRect(masthead.rectTransform, new Vector2(0f, 330f), new Vector2(570f, 58f));

        Image topRule = CreateImage("Top Rule", newspaperObject.transform, new Color32(55, 43, 32, 255));
        SetNewspaperRect(topRule.rectTransform, new Vector2(0f, 288f), new Vector2(550f, 4f));

        TextMeshProUGUI issueLine = CreateNewspaperText(
            "Issue Line",
            newspaperObject.transform,
            "No. 47                         New Bordeaux, April 17, 1927                         Price: 5 Cents",
            15f,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetNewspaperRect(issueLine.rectTransform, new Vector2(0f, 268f), new Vector2(550f, 28f));

        Image issueRule = CreateImage("Issue Rule", newspaperObject.transform, new Color32(55, 43, 32, 255));
        SetNewspaperRect(issueRule.rectTransform, new Vector2(0f, 248f), new Vector2(550f, 3f));

        TextMeshProUGUI headline = CreateNewspaperText(
            "Headline",
            newspaperObject.transform,
            "CAN IT\nACTUALLY REVIVE?",
            62f,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        headline.enableAutoSizing = true;
        headline.fontSizeMin = 44f;
        headline.fontSizeMax = 62f;
        headline.lineSpacing = -18f;
        SetNewspaperRect(headline.rectTransform, new Vector2(-4f, 174f), new Vector2(540f, 128f));

        Image columnRule = CreateImage("Headline Underline", newspaperObject.transform, new Color32(55, 43, 32, 255));
        SetNewspaperRect(columnRule.rectTransform, new Vector2(-154f, 86f), new Vector2(256f, 4f));

        TextMeshProUGUI subHeadline = CreateNewspaperText(
            "Sub Headline",
            newspaperObject.transform,
            "A FORBIDDEN POTION\nSPARKS CONTROVERSY",
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        subHeadline.lineSpacing = -6f;
        SetNewspaperRect(subHeadline.rectTransform, new Vector2(-170f, 34f), new Vector2(300f, 74f));

        TextMeshProUGUI storyLeft = CreateNewspaperText(
            "Story Left",
            newspaperObject.transform,
            "Whispers among alchemists and occultists speak of a potion unlike any other - said to defy death itself.\n\nThe formula remains a secret, guarded by those who claim it holds the power to return a soul to the living world.",
            18f,
            FontStyles.Normal,
            TextAlignmentOptions.Left);
        storyLeft.enableAutoSizing = true;
        storyLeft.fontSizeMin = 14f;
        storyLeft.fontSizeMax = 18f;
        storyLeft.lineSpacing = 2f;
        SetNewspaperRect(storyLeft.rectTransform, new Vector2(-172f, -100f), new Vector2(300f, 188f));

        Texture2D potionTexture = Resources.Load<Texture2D>("Cinematics/Poison");
        if (potionTexture != null)
        {
            Image photoFrame = CreateImage("Potion Photo Frame", newspaperObject.transform, new Color32(65, 50, 35, 255));
            SetNewspaperRect(photoFrame.rectTransform, new Vector2(173f, -30f), new Vector2(235f, 280f));

            Image photoPaper = CreateImage("Potion Photo Paper", newspaperObject.transform, new Color32(188, 165, 124, 255));
            SetNewspaperRect(photoPaper.rectTransform, new Vector2(173f, -30f), new Vector2(223f, 268f));

            GameObject potionObject = new GameObject("Potion Illustration", typeof(RectTransform));
            potionObject.transform.SetParent(newspaperObject.transform, false);
            RawImage potionImage = potionObject.AddComponent<RawImage>();
            potionImage.texture = potionTexture;
            potionImage.color = Color.white;
            potionImage.raycastTarget = false;
            potionImage.uvRect = new Rect(0.02f, 0.13f, 0.96f, 0.74f);
            SetNewspaperRect(potionImage.rectTransform, new Vector2(173f, -30f), new Vector2(205f, 210f));
        }

        Image factBox = CreateImage("Fact Box", newspaperObject.transform, new Color32(165, 140, 100, 70));
        SetNewspaperRect(factBox.rectTransform, new Vector2(-4f, -244f), new Vector2(520f, 88f));

        TextMeshProUGUI fact = CreateNewspaperText(
            "Fact",
            newspaperObject.transform,
            "According to the accounts, the potion's effects can only take hold\nWITHIN 20 DAYS AFTER DYING.",
            20f,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        fact.enableAutoSizing = true;
        fact.fontSizeMin = 15f;
        fact.fontSizeMax = 20f;
        SetNewspaperRect(fact.rectTransform, new Vector2(30f, -244f), new Vector2(468f, 70f));

        TextMeshProUGUI body = CreateNewspaperText(
            "Story",
            newspaperObject.transform,
            "Many have tried. None have succeeded.\nIs this the key to life... or the greatest illusion of all?",
            19f,
            FontStyles.Normal,
            TextAlignmentOptions.Left);
        body.enableAutoSizing = true;
        body.fontSizeMin = 15f;
        body.fontSizeMax = 19f;
        body.lineSpacing = 2f;
        SetNewspaperRect(body.rectTransform, new Vector2(6f, -326f), new Vector2(520f, 74f));
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

    private static Sprite CreateNewspaperPaperSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated Aged Newspaper Paper";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color baseColor = new Color32(197, 174, 132, 255);
        Color stainColor = new Color32(117, 86, 50, 255);
        Color edgeColor = new Color32(83, 61, 38, 255);

        float seedA = 12.73f;
        float seedB = 47.19f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);

                float leftTear = 8f + Mathf.PerlinNoise(ny * 9.5f, seedA) * 24f + Mathf.PerlinNoise(ny * 26f, seedB) * 8f;
                float rightTear = 9f + Mathf.PerlinNoise(ny * 8.2f, seedB) * 22f + Mathf.PerlinNoise(ny * 23f, seedA) * 8f;
                float topTear = 7f + Mathf.PerlinNoise(nx * 10.5f, seedB) * 18f + Mathf.PerlinNoise(nx * 31f, seedA) * 6f;
                float bottomTear = 10f + Mathf.PerlinNoise(nx * 8.5f, seedA) * 26f + Mathf.PerlinNoise(nx * 28f, seedB) * 8f;

                bool outsidePaper =
                    x < leftTear ||
                    x > width - rightTear ||
                    y < bottomTear ||
                    y > height - topTear;

                if (outsidePaper)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float edgeDistance = Mathf.Min(
                    Mathf.Min(x - leftTear, width - rightTear - x),
                    Mathf.Min(y - bottomTear, height - topTear - y));

                float grain = Mathf.PerlinNoise(x * 0.035f, y * 0.035f);
                float fibers = Mathf.PerlinNoise(x * 0.12f, y * 0.018f);
                float stainA = Mathf.PerlinNoise(x * 0.008f + 22f, y * 0.009f + 16f);
                float stainB = Mathf.PerlinNoise(x * 0.018f + 5f, y * 0.016f + 77f);
                float vignette = Mathf.Clamp01(1f - edgeDistance / 70f);
                float stain = Mathf.Clamp01((stainA - 0.52f) * 1.8f + (stainB - 0.62f) * 1.2f);

                Color color = baseColor;
                color *= Mathf.Lerp(0.9f, 1.08f, grain);
                color = Color.Lerp(color, new Color32(229, 208, 163, 255), fibers * 0.16f);
                color = Color.Lerp(color, stainColor, stain * 0.24f);
                color = Color.Lerp(color, edgeColor, vignette * 0.42f);

                float alpha = Mathf.Lerp(0.68f, 1f, Mathf.Clamp01(edgeDistance / 16f));
                color.a = alpha;
                texture.SetPixel(x, y, color);
            }
        }

        AddPaperCrease(texture, width, height, width * 0.52f, true);
        AddPaperCrease(texture, width, height, height * 0.38f, false);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void AddPaperCrease(Texture2D texture, int width, int height, float center, bool vertical)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = Mathf.Abs((vertical ? x : y) - center);
                if (distance > 3f)
                    continue;

                Color color = texture.GetPixel(x, y);
                if (color.a <= 0f)
                    continue;

                float amount = (1f - distance / 3f) * 0.08f;
                color = Color.Lerp(color, new Color32(82, 59, 38, 255), amount);
                texture.SetPixel(x, y, color);
            }
        }
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

    private void CreateClickPrompt(Transform parent)
    {
        GameObject promptObject = new GameObject("Left Click Prompt", typeof(RectTransform), typeof(CanvasGroup));
        promptObject.transform.SetParent(parent, false);

        clickPromptRect = promptObject.GetComponent<RectTransform>();
        clickPromptRect.anchorMin = new Vector2(0.5f, 0f);
        clickPromptRect.anchorMax = new Vector2(0.5f, 0f);
        clickPromptRect.pivot = new Vector2(0.5f, 0f);
        clickPromptRect.anchoredPosition = new Vector2(-80f, 115f);
        clickPromptRect.sizeDelta = new Vector2(240f, 120f);

        clickPromptGroup = promptObject.GetComponent<CanvasGroup>();
        clickPromptGroup.alpha = 0f;
        clickPromptGroup.blocksRaycasts = false;

        GameObject mouseObject = new GameObject("Mouse Left Click Icon", typeof(RectTransform));
        mouseObject.transform.SetParent(promptObject.transform, false);
        Image mouseImage = mouseObject.AddComponent<Image>();
        mouseImage.sprite = CreateMouseClickSprite();
        mouseImage.preserveAspect = true;
        mouseImage.raycastTarget = false;

        RectTransform mouseRect = mouseImage.rectTransform;
        mouseRect.anchorMin = new Vector2(0.5f, 0.5f);
        mouseRect.anchorMax = new Vector2(0.5f, 0.5f);
        mouseRect.pivot = new Vector2(0.5f, 0.5f);
        mouseRect.anchoredPosition = new Vector2(-48f, 0f);
        mouseRect.sizeDelta = new Vector2(70f, 96f);

        TextMeshProUGUI label = CreatePromptText(promptObject.transform);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(70f, 0f);
        labelRect.sizeDelta = new Vector2(180f, 62f);
    }

    private static TextMeshProUGUI CreatePromptText(Transform parent)
    {
        GameObject labelObject = new GameObject("Prompt Text", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "LEFT CLICK";
        label.alignment = TextAlignmentOptions.Left;
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.96f, 0.88f, 0.68f, 1f);
        label.outlineWidth = 0.12f;
        label.outlineColor = new Color32(20, 8, 24, 230);
        label.raycastTarget = false;
        return label;
    }

    private static Sprite CreateMouseClickSprite()
    {
        const int width = 72;
        const int height = 96;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated Mouse Left Click Icon";

        Color clear = Color.clear;
        Color outline = new Color(0.96f, 0.88f, 0.68f, 1f);
        Color fill = new Color(0.08f, 0.04f, 0.1f, 0.88f);
        Color leftButton = new Color(0.86f, 0.25f, 0.2f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                float ny = (y + 0.5f - height * 0.47f) / (height * 0.5f);
                float body = nx * nx * 1.25f + ny * ny;
                bool inside = body <= 0.78f && y > 6 && y < height - 4;
                bool edge = body > 0.66f && body <= 0.78f && y > 6 && y < height - 4;
                bool topSplit = y > height * 0.55f && y < height * 0.88f && Mathf.Abs(x - width * 0.5f) < 1.5f;
                bool leftClickArea = inside && x < width * 0.5f && y > height * 0.56f;
                bool wheel = Mathf.Abs(x - width * 0.5f) < 3.5f && y > height * 0.48f && y < height * 0.62f;

                Color pixel = clear;
                if (inside)
                    pixel = leftClickArea ? leftButton : fill;
                if (edge || topSplit || wheel)
                    pixel = outline;

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
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
        if (Application.isPlaying && !musicHandedOff)
            AudioManager.Instance?.PlayGameplayMusicImmediately();

        if (mainCamera != null)
        {
            mainCamera.transform.position = gameplayCameraPosition;
            mainCamera.orthographicSize = gameplayCameraSize;
        }
    }
}
