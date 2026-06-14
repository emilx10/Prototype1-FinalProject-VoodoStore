using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FamilyMarketUI : MonoBehaviour
{
    private sealed class FamilyPage
    {
        public string characterTexture;
        public string frameTexture;
        public ItemCategory category;

        public FamilyPage(string characterTexture, string frameTexture, ItemCategory category)
        {
            this.characterTexture = characterTexture;
            this.frameTexture = frameTexture;
            this.category = category;
        }
    }

    private static FamilyMarketUI instance;

    private readonly List<FamilyPage> pages = new List<FamilyPage>
    {
        new FamilyPage("Dad", "FrameHerbs", ItemCategory.Herbs),
        new FamilyPage("Mom", "FrameOils", ItemCategory.Oils),
        new FamilyPage("Dota", "FrameStone", ItemCategory.Gems)
    };

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private readonly List<GameObject> itemSlots = new List<GameObject>();

    private GameManager gameManager;
    private GameObject contentRoot;
    private Image characterImage;
    private int pageIndex;

    public static void Attach(GameManager manager)
    {
        if (instance == null)
        {
            GameObject root = new GameObject(
                "Family Market UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.transform.SetParent(manager.transform, false);
            instance = root.AddComponent<FamilyMarketUI>();
            instance.BuildUI();
        }

        instance.gameManager = manager;
        instance.KeepExistingMarketHudVisible();
        instance.RefreshPage();
    }

    public static void RefreshIfVisible()
    {
        if (instance != null && instance.contentRoot.activeSelf)
            instance.RefreshPage();
    }

    private void Update()
    {
        if (gameManager == null || contentRoot == null)
            return;

        bool shouldShow = gameManager.marketPanel != null &&
            gameManager.marketPanel.activeInHierarchy &&
            !gameManager.craftingPanel.activeInHierarchy &&
            !gameManager.sellPanel.activeInHierarchy &&
            !gameManager.IsKnownRecipesOpen();

        if (contentRoot.activeSelf != shouldShow)
        {
            contentRoot.SetActive(shouldShow);
            if (shouldShow)
                RefreshPage();
        }

        if (shouldShow)
            gameManager.PrepareBookCanvasForFamilyMarket();
    }

    private void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 10f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        contentRoot = CreateRect("Family Market Content", transform, Vector2.zero, Vector2.zero);
        Stretch(contentRoot.GetComponent<RectTransform>());

        Image background = CreateImage("Shop Background", contentRoot.transform, LoadSprite("Shop"));
        Stretch(background.rectTransform);
        background.raycastTarget = true;

        characterImage = CreateImage("Family Member", contentRoot.transform, null);
        RectTransform characterRect = characterImage.rectTransform;
        characterRect.anchorMin = new Vector2(0.5f, 0.5f);
        characterRect.anchorMax = new Vector2(0.5f, 0.5f);
        characterRect.pivot = new Vector2(0.5f, 0.5f);
        characterRect.anchoredPosition = new Vector2(0f, 75f);
        characterRect.sizeDelta = new Vector2(650f, 760f);
        characterImage.preserveAspect = true;
        characterImage.raycastTarget = false;

        Image desk = CreateImage("Desk", contentRoot.transform, LoadSprite("Desk"));
        RectTransform deskRect = desk.rectTransform;
        deskRect.anchorMin = new Vector2(0f, 0f);
        deskRect.anchorMax = new Vector2(1f, 0f);
        deskRect.pivot = new Vector2(0.5f, 0f);
        deskRect.anchoredPosition = Vector2.zero;
        deskRect.sizeDelta = new Vector2(0f, 320f);
        desk.raycastTarget = false;

        CreateArrow(new Vector2(-855f, 25f), -1f, -1);
        CreateArrow(new Vector2(855f, 25f), 1f, 1);

        for (int i = 0; i < 3; i++)
            itemSlots.Add(CreateItemSlot(i));

        CreateCommandButton(
            "Enter Shop",
            new Vector2(-765f, -465f),
            new Vector2(250f, 70f),
            "Enter shop",
            () => gameManager?.InvokeMarketShopButton());

        contentRoot.SetActive(false);
    }

    private void CreateArrow(Vector2 position, float horizontalScale, int direction)
    {
        GameObject arrowObject = CreateRect("Family Arrow", contentRoot.transform, position, new Vector2(110f, 110f));
        Image image = arrowObject.AddComponent<Image>();
        image.sprite = LoadSprite("Arrow");
        image.preserveAspect = true;

        Button button = arrowObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ChangePage(direction));

        RectTransform rect = arrowObject.GetComponent<RectTransform>();
        rect.localScale = new Vector3(horizontalScale, 1f, 1f);
    }

    private GameObject CreateItemSlot(int slotIndex)
    {
        float x = -350f + slotIndex * 350f;
        GameObject slot = CreateRect(
            $"Market Item Slot {slotIndex + 1}",
            contentRoot.transform,
            new Vector2(x, -345f),
            new Vector2(300f, 235f));

        Image background = slot.AddComponent<Image>();
        background.sprite = LoadSprite("MaterialSlot");
        background.preserveAspect = false;

        Button button = slot.AddComponent<Button>();
        button.targetGraphic = background;

        Image frame = CreateImage("Category Frame", slot.transform, null);
        Stretch(frame.rectTransform);
        frame.raycastTarget = false;
        frame.preserveAspect = false;

        Image icon = CreateImage("Icon", slot.transform, null);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 22f);
        iconRect.sizeDelta = new Vector2(130f, 130f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        CreateText(
            "Item Name",
            slot.transform,
            string.Empty,
            new Vector2(0f, -67f),
            new Vector2(260f, 34f),
            22f,
            TextAlignmentOptions.Center);

        CreateText(
            "Item Details",
            slot.transform,
            string.Empty,
            new Vector2(0f, -98f),
            new Vector2(260f, 30f),
            18f,
            TextAlignmentOptions.Center);

        return slot;
    }

    private void RefreshPage()
    {
        if (gameManager == null || characterImage == null)
            return;

        FamilyPage page = pages[pageIndex];
        characterImage.sprite = LoadSprite(page.characterTexture);

        Market market = gameManager.GetMarketForCategory(page.category);
        Sprite frameSprite = LoadSprite(page.frameTexture);

        for (int i = 0; i < itemSlots.Count; i++)
        {
            GameObject slot = itemSlots[i];
            bool hasItem = market != null && i < market.items.Count;
            slot.SetActive(hasItem);

            if (!hasItem)
                continue;

            MarketItem item = market.items[i];
            int stock = gameManager.GetMarketStock(item);

            slot.transform.Find("Category Frame").GetComponent<Image>().sprite = frameSprite;
            slot.transform.Find("Icon").GetComponent<Image>().sprite = item.icon;
            slot.transform.Find("Item Name").GetComponent<TMP_Text>().text = item.itemName;
            slot.transform.Find("Item Details").GetComponent<TMP_Text>().text =
                $"{item.price} coins   x{stock}";

            Button button = slot.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.interactable = stock > 0 && gameManager.coins >= item.price;
            button.onClick.AddListener(() =>
            {
                gameManager.BuyMarketItemFromFamilyUI(item);
                RefreshPage();
            });

            ItemHoverTooltip tooltip = slot.GetComponent<ItemHoverTooltip>();
            if (tooltip == null)
                tooltip = slot.AddComponent<ItemHoverTooltip>();
            tooltip.marketItem = item;
        }
    }

    private void KeepExistingMarketHudVisible()
    {
        if (gameManager.coinsText == null || gameManager.coinsText.transform.parent == null)
            return;

        GameObject coinGraphics = gameManager.coinsText.transform.parent.gameObject;
        Canvas coinCanvas = coinGraphics.GetComponent<Canvas>();
        if (coinCanvas == null)
            coinCanvas = coinGraphics.AddComponent<Canvas>();

        coinCanvas.overrideSorting = true;
        coinCanvas.sortingOrder = 110;
    }

    private void ChangePage(int direction)
    {
        pageIndex = (pageIndex + direction + pages.Count) % pages.Count;
        RefreshPage();
    }

    private void CreateCommandButton(
        string objectName,
        Vector2 position,
        Vector2 size,
        string labelText,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateRect(objectName, contentRoot.transform, position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.24f, 0.08f, 0.055f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        CreateText(
            "Label",
            buttonObject.transform,
            labelText,
            Vector2.zero,
            size,
            25f,
            TextAlignmentOptions.Center);
    }

    private Sprite LoadSprite(string resourceName)
    {
        if (spriteCache.TryGetValue(resourceName, out Sprite sprite))
            return sprite;

        Texture2D texture = Resources.Load<Texture2D>($"FamilyMarket/{resourceName}");
        if (texture == null)
        {
            Debug.LogError($"Missing Family Market texture: {resourceName}");
            return null;
        }

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = resourceName;
        spriteCache.Add(resourceName, sprite);
        return sprite;
    }

    private static GameObject CreateRect(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static Image CreateImage(string objectName, Transform parent, Sprite sprite)
    {
        GameObject obj = CreateRect(objectName, parent, Vector2.zero, Vector2.zero);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject obj = CreateRect(objectName, parent, position, size);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(1f, 0.9f, 0.68f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
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
