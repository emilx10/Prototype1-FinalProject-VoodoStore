using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class FamilyMarketUI : MonoBehaviour
{
    private enum RightPanelTab
    {
        Objectives,
        Inventory,
        KnownRecipes
    }

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
        new FamilyPage("Dad", "seller", ItemCategory.Herbs),
        new FamilyPage("Mom", "Flower Frame", ItemCategory.Oils),
        new FamilyPage("Dota", "Crystal Frame", ItemCategory.Gems)
    };

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private readonly List<GameObject> itemSlots = new List<GameObject>();
    private readonly Dictionary<GameObject, Coroutine> activePurchaseVfx = new Dictionary<GameObject, Coroutine>();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image dadCharacterImage;
    [SerializeField] private Image momCharacterImage;
    [SerializeField] private Image dotaCharacterImage;
    [SerializeField] private Image rightUiBlockImage;
    [SerializeField] private RectTransform leftArrowRect;
    [SerializeField] private RectTransform rightArrowRect;
    [SerializeField] private RectTransform inventoryButtonRect;
    [SerializeField] private Image inventoryButtonImage;
    [SerializeField] private RectTransform rightPanelTabsRoot;
    [SerializeField] private RectTransform rightPanelContentRoot;
    [SerializeField] private Button objectivesTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button knownRecipesTabButton;
    [SerializeField] private GameObject objectivesTabContent;
    [SerializeField] private GameObject inventoryTabContent;
    [SerializeField] private GameObject knownRecipesTabContent;
    [SerializeField] private RightPanelTab activeRightPanelTab = RightPanelTab.Objectives;
    private int pageIndex;

    public static void Attach(GameManager manager)
    {
        if (instance == null)
            instance = FindFirstObjectByType<FamilyMarketUI>(FindObjectsInactive.Include);

        if (instance == null)
            instance = CreateFamilyMarketUIObject(manager.transform);

        instance.gameManager = manager;
        instance.BuildUI();
        instance.KeepExistingMarketHudVisible();
        instance.RefreshPage();
    }

    [ContextMenu("Build / Refresh Editable Preview")]
    public void BuildEditablePreview()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

        BuildUI();
        CacheBuiltChildren();

        if (contentRoot != null)
            contentRoot.SetActive(true);

        RefreshPage();
    }

    private static FamilyMarketUI CreateFamilyMarketUIObject(Transform parent)
    {
        GameObject root = new GameObject(
            "Family Market UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        if (parent != null)
            root.transform.SetParent(parent, false);

        FamilyMarketUI ui = root.AddComponent<FamilyMarketUI>();
        ui.BuildUI();
        return ui;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Family Market/Create Editable UI")]
    private static void CreateEditableFamilyMarketUI()
    {
        FamilyMarketUI ui = FindFirstObjectByType<FamilyMarketUI>(FindObjectsInactive.Include);
        GameManager manager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

        if (ui == null)
            ui = CreateFamilyMarketUIObject(manager != null ? manager.transform : null);

        ui.gameManager = manager;
        ui.BuildEditablePreview();
        UnityEditor.Selection.activeGameObject = ui.gameObject;
        UnityEditor.EditorUtility.SetDirty(ui.gameObject);

        if (ui.contentRoot != null)
            UnityEditor.EditorUtility.SetDirty(ui.contentRoot);
    }
#endif

    public static void RefreshIfVisible()
    {
        if (instance != null && instance.contentRoot != null && instance.contentRoot.activeSelf)
            instance.RefreshPage();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

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
        {
            ApplyFamilyMarketLayout();
            gameManager.PrepareBookCanvasForFamilyMarket();
        }
    }

    private void BuildUI()
    {
        CacheBuiltChildren();
        if (contentRoot != null)
        {
            EnsureCharacterImages();
            EnsureFamilyMarketControls();
            EnsureRightPanelTabs();
            return;
        }

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

        dadCharacterImage = CreateCharacterImage("Dad Family Member", "Dad");
        momCharacterImage = CreateCharacterImage("Mom Family Member", "Mom");
        dotaCharacterImage = CreateCharacterImage("Dota Family Member", "Dota");
        characterImage = dadCharacterImage;

        Image desk = CreateImage("Desk", contentRoot.transform, LoadSprite("Desk"));
        RectTransform deskRect = desk.rectTransform;
        deskRect.anchorMin = new Vector2(0f, 0f);
        deskRect.anchorMax = new Vector2(1f, 0f);
        deskRect.pivot = new Vector2(0.5f, 0f);
        deskRect.anchoredPosition = Vector2.zero;
        deskRect.sizeDelta = new Vector2(0f, 320f);
        desk.raycastTarget = false;
        ConfigureDeskFrontLayer(desk.gameObject);

        rightUiBlockImage = CreateImage("Seller Right UI Block", contentRoot.transform, LoadSprite("SellerRightUI"));
        rightUiBlockImage.preserveAspect = true;
        rightUiBlockImage.raycastTarget = false;
        EnsureRightPanelTabs();

        leftArrowRect = CreateArrow(-1f, -1);
        rightArrowRect = CreateArrow(1f, 1);
        ApplyFamilyMarketLayout();

        for (int i = 0; i < 3; i++)
            itemSlots.Add(CreateItemSlot(i));

        CreateCommandButton(
            "Enter Shop",
            new Vector2(-765f, -465f),
            new Vector2(250f, 70f),
            "Enter shop",
            () => gameManager?.InvokeMarketShopButton());

        contentRoot.SetActive(!Application.isPlaying);
        CacheBuiltChildren();
    }

    private void EnsureCharacterImages()
    {
        if (contentRoot == null)
            return;

        if (dadCharacterImage == null)
            dadCharacterImage = CreateCharacterImage("Dad Family Member", "Dad");

        if (momCharacterImage == null)
            momCharacterImage = CreateCharacterImage("Mom Family Member", "Mom");

        if (dotaCharacterImage == null)
            dotaCharacterImage = CreateCharacterImage("Dota Family Member", "Dota");

        if (characterImage == null)
            characterImage = dadCharacterImage;
    }

    private RectTransform CreateArrow(float horizontalScale, int direction)
    {
        GameObject arrowObject = CreateRect("Family Arrow", contentRoot.transform, Vector2.zero, new Vector2(86f, 86f));
        Image image = arrowObject.AddComponent<Image>();
        image.sprite = LoadSprite("Arrow");
        image.preserveAspect = true;

        Button button = arrowObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ChangePage(direction));

        RectTransform rect = arrowObject.GetComponent<RectTransform>();
        rect.localScale = new Vector3(horizontalScale, 1f, 1f);
        return rect;
    }

    private void CreateInventoryButton()
    {
        GameObject buttonObject = CreateRect(
            "Family Market Inventory Button",
            contentRoot.transform,
            Vector2.zero,
            new Vector2(120f, 120f));

        inventoryButtonRect = buttonObject.GetComponent<RectTransform>();
        inventoryButtonImage = buttonObject.AddComponent<Image>();
        inventoryButtonImage.sprite = gameManager != null ? gameManager.FamilyMarketInventoryIcon : null;
        inventoryButtonImage.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = inventoryButtonImage;
        button.onClick.AddListener(() => ShowRightPanelTab(RightPanelTab.Inventory));
    }

    private void EnsureFamilyMarketControls()
    {
        CacheBuiltChildren();

        if (leftArrowRect != null)
            ConfigureArrowButton(leftArrowRect, -1);

        if (rightArrowRect != null)
            ConfigureArrowButton(rightArrowRect, 1);

        ConfigureInventoryButton();
        EnsureRightPanelTabs();
        ApplyFamilyMarketLayout();
    }

    private void ConfigureArrowButton(RectTransform arrowRect, int direction)
    {
        Button button = arrowRect.GetComponent<Button>();
        Image image = arrowRect.GetComponent<Image>();

        if (button == null)
            button = arrowRect.gameObject.AddComponent<Button>();

        if (image != null)
        {
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ChangePage(direction));
        button.interactable = true;
    }

    private void ConfigureInventoryButton()
    {
        if (inventoryButtonRect == null)
            return;

        if (inventoryButtonImage == null)
            inventoryButtonImage = inventoryButtonRect.GetComponent<Image>();

        if (inventoryButtonImage == null)
            inventoryButtonImage = inventoryButtonRect.gameObject.AddComponent<Image>();

        inventoryButtonImage.sprite = gameManager != null ? gameManager.FamilyMarketInventoryIcon : null;
        inventoryButtonImage.preserveAspect = true;
        inventoryButtonImage.raycastTarget = true;

        Button button = inventoryButtonRect.GetComponent<Button>();
        if (button == null)
            button = inventoryButtonRect.gameObject.AddComponent<Button>();

        button.targetGraphic = inventoryButtonImage;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowRightPanelTab(RightPanelTab.Inventory));
        button.interactable = true;
    }

    private void EnsureRightPanelTabs()
    {
        if (rightUiBlockImage == null)
            return;

        if (rightPanelTabsRoot == null)
            rightPanelTabsRoot = rightUiBlockImage.transform.Find("Right Panel Tabs Root")?.GetComponent<RectTransform>();

        if (rightPanelTabsRoot == null)
        {
            GameObject root = CreateRect("Right Panel Tabs Root", rightUiBlockImage.transform, Vector2.zero, Vector2.zero);
            rightPanelTabsRoot = root.GetComponent<RectTransform>();
            rightPanelTabsRoot.anchorMin = new Vector2(0.12f, 0.11f);
            rightPanelTabsRoot.anchorMax = new Vector2(0.88f, 0.86f);
            rightPanelTabsRoot.offsetMin = Vector2.zero;
            rightPanelTabsRoot.offsetMax = Vector2.zero;
        }

        if (rightPanelContentRoot == null)
            rightPanelContentRoot = rightPanelTabsRoot.Find("Right Panel Content Root")?.GetComponent<RectTransform>();

        if (rightPanelContentRoot == null)
        {
            GameObject content = CreateRect("Right Panel Content Root", rightPanelTabsRoot, Vector2.zero, Vector2.zero);
            rightPanelContentRoot = content.GetComponent<RectTransform>();
            Stretch(rightPanelContentRoot);
            rightPanelContentRoot.offsetMin = Vector2.zero;
            rightPanelContentRoot.offsetMax = new Vector2(0f, -78f);
        }

        DisableGeneratedButton("Objectives Tab Button");
        DisableGeneratedButton("Inventory Tab Button");
        DisableGeneratedButton("Known Recipes Tab Button");
        DisableGeneratedButton("Family Market Inventory Button");

        objectivesTabButton = FindExistingTabButton(objectivesTabButton, "objective");
        inventoryTabButton = FindExistingTabButton(inventoryTabButton, "inventory");
        knownRecipesTabButton = FindExistingTabButton(knownRecipesTabButton, "recipe");

        ConfigureExistingTabButton(objectivesTabButton, RightPanelTab.Objectives);
        ConfigureExistingTabButton(inventoryTabButton, RightPanelTab.Inventory);
        ConfigureExistingTabButton(knownRecipesTabButton, RightPanelTab.KnownRecipes);

        objectivesTabContent = EnsureTabContent(objectivesTabContent, "Objectives Tab Content", "Objectives");
        inventoryTabContent = EnsureTabContent(inventoryTabContent, "Inventory Tab Content", "Inventory");
        knownRecipesTabContent = EnsureTabContent(knownRecipesTabContent, "Known Recipes Tab Content", "Known Recipes");

        RefreshRightPanelTabs();
    }

    private Button FindExistingTabButton(Button currentButton, string namePart)
    {
        if (IsUsableUserButton(currentButton))
            return currentButton;

        return FindButtonByNamePart(contentRoot != null ? contentRoot.transform : transform, namePart);
    }

    private void ConfigureExistingTabButton(Button button, RightPanelTab tab)
    {
        if (!IsUsableUserButton(button))
            return;

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowRightPanelTab(tab));
        button.interactable = true;
    }

    private bool IsUsableUserButton(Button button)
    {
        return button != null &&
            button.gameObject.activeSelf &&
            !IsGeneratedButtonName(button.gameObject.name);
    }

    private Button FindButtonByNamePart(Transform root, string namePart)
    {
        if (root == null)
            return null;

        string lowerNamePart = namePart.ToLowerInvariant();
        Button[] buttons = root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            string lowerName = button.gameObject.name.ToLowerInvariant();

            if (IsGeneratedButtonName(button.gameObject.name))
                continue;

            if (lowerName.Contains(lowerNamePart))
                return button;
        }

        return null;
    }

    private void DisableGeneratedButton(string objectName)
    {
        if (contentRoot == null)
            return;

        Transform generated = FindDeepChild(contentRoot.transform, objectName);
        if (generated != null)
            generated.gameObject.SetActive(false);
    }

    private static bool IsGeneratedButtonName(string objectName)
    {
        return objectName == "Objectives Tab Button" ||
            objectName == "Inventory Tab Button" ||
            objectName == "Known Recipes Tab Button" ||
            objectName == "Family Market Inventory Button";
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private GameObject EnsureTabContent(GameObject content, string objectName, string placeholderText)
    {
        if (content == null && rightPanelContentRoot != null)
        {
            Transform existing = rightPanelContentRoot.Find(objectName);
            if (existing != null)
                content = existing.gameObject;
        }

        if (content == null)
        {
            content = CreateRect(objectName, rightPanelContentRoot, Vector2.zero, Vector2.zero);
            RectTransform rect = content.GetComponent<RectTransform>();
            Stretch(rect);

            TMP_Text text = CreateText(
                "Placeholder",
                content.transform,
                placeholderText,
                Vector2.zero,
                new Vector2(600f, 140f),
                38f,
                TextAlignmentOptions.Center);
            text.color = new Color(0.25f, 0.09f, 0.06f, 0.88f);
        }

        return content;
    }

    private void ShowRightPanelTab(RightPanelTab tab)
    {
        activeRightPanelTab = tab;
        RefreshRightPanelTabs();
    }

    private void RefreshRightPanelTabs()
    {
        SetTabContentActive(objectivesTabContent, activeRightPanelTab == RightPanelTab.Objectives);
        SetTabContentActive(inventoryTabContent, activeRightPanelTab == RightPanelTab.Inventory);
        SetTabContentActive(knownRecipesTabContent, activeRightPanelTab == RightPanelTab.KnownRecipes);

        ApplyTabButtonVisual(objectivesTabButton, activeRightPanelTab == RightPanelTab.Objectives);
        ApplyTabButtonVisual(inventoryTabButton, activeRightPanelTab == RightPanelTab.Inventory);
        ApplyTabButtonVisual(knownRecipesTabButton, activeRightPanelTab == RightPanelTab.KnownRecipes);
    }

    private static void SetTabContentActive(GameObject content, bool active)
    {
        if (content != null && content.activeSelf != active)
            content.SetActive(active);
    }

    private static void ApplyTabButtonVisual(Button button, bool active)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();

        if (image == null)
            return;

        image.color = active
            ? new Color(0.62f, 0.24f, 0.16f, 0.96f)
            : new Color(0.24f, 0.09f, 0.07f, 0.82f);
    }

    private Image CreateCharacterImage(string objectName, string spriteName)
    {
        Image image = CreateImage(objectName, contentRoot.transform, LoadSprite(spriteName));
        SetupCharacterRect(image.rectTransform);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }

    private static void SetupCharacterRect(RectTransform characterRect)
    {
        characterRect.anchorMin = new Vector2(0.5f, 0.5f);
        characterRect.anchorMax = new Vector2(0.5f, 0.5f);
        characterRect.pivot = new Vector2(0.5f, 0.5f);
        characterRect.anchoredPosition = new Vector2(0f, 75f);
        characterRect.sizeDelta = new Vector2(650f, 760f);
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
        ConfigureFrontUiLayer(slot, 120);

        Image frame = CreateImage("Category Frame", slot.transform, null);
        Stretch(frame.rectTransform);
        frame.raycastTarget = false;
        frame.preserveAspect = true;

        Image purchaseFlash = CreateImage("Purchase Frame Flash", slot.transform, null);
        Stretch(purchaseFlash.rectTransform);
        purchaseFlash.raycastTarget = false;
        purchaseFlash.preserveAspect = true;
        purchaseFlash.color = Color.clear;

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
        if (gameManager == null)
            return;

        ApplyFamilyMarketLayout();
        EnsureRightPanelTabs();

        FamilyPage page = pages[pageIndex];
        ApplyCharacterVisibility(page.characterTexture);

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
            slot.transform.Find("Purchase Frame Flash").GetComponent<Image>().sprite = frameSprite;
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
                PlayItemPurchaseVfx(slot);
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

    private void ApplyCharacterVisibility(string characterTexture)
    {
        SetImageActive(dadCharacterImage, characterTexture == "Dad");
        SetImageActive(momCharacterImage, characterTexture == "Mom");
        SetImageActive(dotaCharacterImage, characterTexture == "Dota");

        if (characterImage != null &&
            characterImage != dadCharacterImage &&
            characterImage != momCharacterImage &&
            characterImage != dotaCharacterImage)
        {
            characterImage.gameObject.SetActive(false);
        }
    }

    private static void SetImageActive(Image image, bool active)
    {
        if (image != null && image.gameObject.activeSelf != active)
            image.gameObject.SetActive(active);
    }

    private void CacheBuiltChildren()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("Family Market Content");
            if (content != null)
                contentRoot = content.gameObject;
        }

        if (contentRoot == null)
            return;

        Transform contentTransform = contentRoot.transform;

        if (characterImage == null)
            characterImage = contentTransform.Find("Family Member")?.GetComponent<Image>();

        if (dadCharacterImage == null)
            dadCharacterImage = contentTransform.Find("Dad Family Member")?.GetComponent<Image>();

        if (momCharacterImage == null)
            momCharacterImage = contentTransform.Find("Mom Family Member")?.GetComponent<Image>();

        if (dotaCharacterImage == null)
            dotaCharacterImage = contentTransform.Find("Dota Family Member")?.GetComponent<Image>();

        if (characterImage == null)
            characterImage = dadCharacterImage;

        if (rightUiBlockImage == null)
            rightUiBlockImage = contentTransform.Find("Seller Right UI Block")?.GetComponent<Image>();

        if (rightUiBlockImage != null)
        {
            if (rightPanelTabsRoot == null)
                rightPanelTabsRoot = rightUiBlockImage.transform.Find("Right Panel Tabs Root")?.GetComponent<RectTransform>();

            if (rightPanelTabsRoot != null)
            {
                if (rightPanelContentRoot == null)
                    rightPanelContentRoot = rightPanelTabsRoot.Find("Right Panel Content Root")?.GetComponent<RectTransform>();

                if (rightPanelContentRoot != null)
                {
                    if (objectivesTabContent == null)
                        objectivesTabContent = rightPanelContentRoot.Find("Objectives Tab Content")?.gameObject;

                    if (inventoryTabContent == null)
                        inventoryTabContent = rightPanelContentRoot.Find("Inventory Tab Content")?.gameObject;

                    if (knownRecipesTabContent == null)
                        knownRecipesTabContent = rightPanelContentRoot.Find("Known Recipes Tab Content")?.gameObject;
                }
            }
        }

        DisableGeneratedButton("Family Market Inventory Button");
        ConfigureDeskFrontLayer(contentTransform.Find("Desk")?.gameObject);

        if (leftArrowRect == null || rightArrowRect == null)
        {
            List<RectTransform> arrows = new List<RectTransform>();
            for (int i = 0; i < contentTransform.childCount; i++)
            {
                Transform child = contentTransform.GetChild(i);
                if (child.name == "Family Arrow" && child is RectTransform rect)
                    arrows.Add(rect);
            }

            if (leftArrowRect == null && arrows.Count > 0)
                leftArrowRect = arrows[0];

            if (rightArrowRect == null && arrows.Count > 1)
                rightArrowRect = arrows[1];
        }

        itemSlots.Clear();
        for (int i = 1; i <= 3; i++)
        {
            Transform slot = contentTransform.Find($"Market Item Slot {i}");
            if (slot != null)
            {
                ConfigureFrontUiLayer(slot.gameObject, 120);
                itemSlots.Add(slot.gameObject);
            }
        }
    }

    private void ChangePage(int direction)
    {
        pageIndex = (pageIndex + direction + pages.Count) % pages.Count;
        RefreshPage();
    }

    private void PlayItemPurchaseVfx(GameObject slot)
    {
        if (slot == null || !slot.activeInHierarchy)
            return;

        if (activePurchaseVfx.TryGetValue(slot, out Coroutine running) && running != null)
            StopCoroutine(running);

        activePurchaseVfx[slot] = StartCoroutine(PlayItemPurchaseVfxRoutine(slot));
    }

    private IEnumerator PlayItemPurchaseVfxRoutine(GameObject slot)
    {
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        Image frame = slot.transform.Find("Category Frame")?.GetComponent<Image>();
        Image flash = slot.transform.Find("Purchase Frame Flash")?.GetComponent<Image>();
        RectTransform iconRect = slot.transform.Find("Icon")?.GetComponent<RectTransform>();

        if (slotRect == null || frame == null || flash == null || iconRect == null)
            yield break;

        Vector3 slotStartScale = slotRect.localScale;
        Vector3 iconStartScale = iconRect.localScale;
        Color frameStartColor = frame.color;
        Color flashColor = new Color(1f, 0.86f, 0.24f, 0f);
        float duration = 0.42f;
        float elapsed = 0f;

        flash.transform.SetAsLastSibling();

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float snap = 1f - Mathf.Pow(1f - t, 3f);

            slotRect.localScale = slotStartScale * (1f + pulse * 0.07f);
            iconRect.localScale = iconStartScale * (1f + pulse * 0.16f);
            frame.color = Color.Lerp(frameStartColor, new Color(1f, 0.94f, 0.46f, frameStartColor.a), pulse);

            flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.04f, 1.22f, snap);
            flashColor.a = Mathf.Lerp(0.62f, 0f, snap);
            flash.color = flashColor;
            flash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-4f, 4f, pulse));

            yield return null;
        }

        slotRect.localScale = slotStartScale;
        iconRect.localScale = iconStartScale;
        frame.color = frameStartColor;
        flash.color = Color.clear;
        flash.rectTransform.localScale = Vector3.one;
        flash.rectTransform.localRotation = Quaternion.identity;
        activePurchaseVfx.Remove(slot);
    }

    private void ApplyFamilyMarketLayout()
    {
        if (gameManager == null)
            return;

        ApplyRightUiBlockLayout();
        ApplyArrowLayout(leftArrowRect, gameManager.FamilyMarketLeftArrowPosition, -1f);
        ApplyArrowLayout(rightArrowRect, gameManager.FamilyMarketRightArrowPosition, 1f);
        ApplyInventoryButtonLayout();
    }

    private void ConfigureDeskFrontLayer(GameObject deskObject)
    {
        ConfigureFrontUiLayer(deskObject, 110);
    }

    private void ConfigureFrontUiLayer(GameObject uiObject, int sortingOrder)
    {
        if (uiObject == null)
            return;

        Canvas canvas = uiObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = uiObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }

    private void ApplyRightUiBlockLayout()
    {
        if (rightUiBlockImage == null || gameManager == null)
            return;

        RectTransform rect = rightUiBlockImage.rectTransform;
        SetCenteredRect(rect);
        rect.anchoredPosition = gameManager.FamilyMarketRightUiPosition;
        rect.sizeDelta = gameManager.FamilyMarketRightUiSize;
        rect.localScale = gameManager.FamilyMarketRightUiScale;
        rect.localRotation = Quaternion.Euler(0f, 0f, gameManager.FamilyMarketRightUiRotation);
    }

    private void ApplyInventoryButtonLayout()
    {
        if (inventoryButtonRect == null || gameManager == null)
            return;

        SetCenteredRect(inventoryButtonRect);
        inventoryButtonRect.anchoredPosition = gameManager.FamilyMarketInventoryButtonPosition;
        inventoryButtonRect.sizeDelta = gameManager.FamilyMarketInventoryButtonSize;
        inventoryButtonRect.localScale = gameManager.FamilyMarketInventoryButtonScale;
        inventoryButtonRect.localRotation = Quaternion.Euler(0f, 0f, gameManager.FamilyMarketInventoryButtonRotation);
        inventoryButtonRect.SetAsLastSibling();
    }

    private void ApplyArrowLayout(RectTransform arrowRect, Vector2 position, float horizontalDirection)
    {
        if (arrowRect == null || gameManager == null)
            return;

        SetCenteredRect(arrowRect);
        arrowRect.anchoredPosition = position;
        arrowRect.sizeDelta = gameManager.FamilyMarketArrowSize;
        arrowRect.localScale = Vector3.Scale(
            gameManager.FamilyMarketArrowScale,
            new Vector3(horizontalDirection, 1f, 1f));
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, gameManager.FamilyMarketArrowRotation);
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

        string resourcePath = $"FamilyMarket/{resourceName}";

        Sprite directSprite = Resources.Load<Sprite>(resourcePath);
        if (directSprite != null)
        {
            spriteCache.Add(resourceName, directSprite);
            return directSprite;
        }

        Sprite[] slicedSprites = Resources.LoadAll<Sprite>(resourcePath);
        if (slicedSprites != null && slicedSprites.Length > 0)
        {
            sprite = slicedSprites[0];

            for (int i = 0; i < slicedSprites.Length; i++)
            {
                if (slicedSprites[i].name == resourceName || slicedSprites[i].name == resourceName + "_0")
                {
                    sprite = slicedSprites[i];
                    break;
                }
            }

            spriteCache.Add(resourceName, sprite);
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
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

    private static void SetCenteredRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetTopCenteredRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
