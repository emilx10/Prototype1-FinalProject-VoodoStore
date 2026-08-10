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
    private const string GeneratedObjectiveContentName = "Generated Objectives Content";
    private const string GeneratedInventoryContentName = "Generated Inventory Content";
    private const string InventoryScrollViewName = "Inventory Scroll View";
    private const string InventoryScrollViewportName = "Inventory Scroll Viewport";
    private const string InventoryScrollContentName = "Inventory Scroll Content";
    private const string GeneratedKnownRecipesContentName = "Generated Known Recipes Content";
    private const string KnownRecipesScrollViewName = "Known Recipes Scroll View";
    private const string KnownRecipesScrollViewportName = "Known Recipes Scroll Viewport";
    private const string KnownRecipesScrollContentName = "Known Recipes Scroll Content";
    private const int DefaultEditableInventoryRowCount = 12;
    private const int DefaultEditableKnownRecipeCardCount = 12;
    private const float InventoryRowSpacing = 86f;
    private const float KnownRecipeCardSpacing = 230f;
    private const int RightPanelSortingOrder = 220;
    private const int RightPanelButtonSortingOrder = 240;

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
    [SerializeField] private Button enterShopButton;
    [SerializeField] private Button objectivesTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button knownRecipesTabButton;
    [SerializeField] private GameObject objectivesTabContent;
    [SerializeField] private GameObject inventoryTabContent;
    [SerializeField] private GameObject knownRecipesTabContent;
    [SerializeField] private RightPanelTab activeRightPanelTab = RightPanelTab.Objectives;
    [SerializeField] private bool preserveManualLayout = true;
    [SerializeField] private int editableInventoryRowCount = DefaultEditableInventoryRowCount;
    [SerializeField] private int editableKnownRecipeCardCount = DefaultEditableKnownRecipeCardCount;
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
        EnsureInventoryEditablePreview();
        EnsureKnownRecipesEditablePreview();
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
            gameManager.PrepareBookCanvasForFamilyMarket();
        }
    }

    private void BuildUI()
    {
        CacheBuiltChildren();
        if (!preserveManualLayout || contentRoot == null)
            ConfigureRootCanvas();

        if (contentRoot != null)
        {
            DisableGeneratedFamilyMembers();
            EnsureFamilyMarketControls();
            EnsureRightPanelTabs();
            return;
        }

        contentRoot = CreateRect("Family Market Content", transform, Vector2.zero, Vector2.zero);
        Stretch(contentRoot.GetComponent<RectTransform>());

        Image background = CreateImage("Shop Background", contentRoot.transform, LoadSprite("Shop"));
        Stretch(background.rectTransform);
        background.raycastTarget = true;

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

    private void ConfigureRootCanvas()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
            Stretch(rootRect);

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 10f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureCharacterImages()
    {
        if (contentRoot == null)
            return;

        DisableGeneratedFamilyMembers();
    }

    private void DisableGeneratedFamilyMembers()
    {
        if (contentRoot == null)
            return;

        DisableGeneratedImage("Dad Family Member", ref dadCharacterImage);
        DisableGeneratedImage("Mom Family Member", ref momCharacterImage);
        DisableGeneratedImage("Dota Family Member", ref dotaCharacterImage);
        DisableGeneratedImage("Family Member", ref characterImage);
    }

    private void DisableGeneratedImage(string objectName, ref Image image)
    {
        Transform generated = FindDeepChild(contentRoot.transform, objectName);
        if (generated != null)
            generated.gameObject.SetActive(false);

        if (image != null && image.gameObject.name == objectName)
            image = null;
    }

    private RectTransform CreateArrow(float horizontalScale, int direction)
    {
        GameObject arrowObject = CreateRect(
            direction < 0 ? "Family Arrow Left" : "Family Arrow Right",
            contentRoot.transform,
            Vector2.zero,
            new Vector2(86f, 86f));
        Image image = arrowObject.AddComponent<Image>();
        image.sprite = LoadSprite("Arrow");
        image.preserveAspect = true;
        image.raycastTarget = true;

        Button button = arrowObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ChangePage(direction));
        ConfigureFrontUiLayer(arrowObject, 140);

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
        ConfigureEnterShopButton();
        EnsureRightPanelTabs();
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
        arrowRect.gameObject.SetActive(true);
        ConfigureArrowFrontLayer(arrowRect);
    }

    private void ConfigureInventoryButton()
    {
        if (inventoryButtonRect == null)
            return;

        if (inventoryButtonImage == null)
            inventoryButtonImage = inventoryButtonRect.GetComponent<Image>();

        if (inventoryButtonImage == null)
            inventoryButtonImage = inventoryButtonRect.gameObject.AddComponent<Image>();

        inventoryButtonImage.preserveAspect = true;
        inventoryButtonImage.raycastTarget = true;

        Button button = inventoryButtonRect.GetComponent<Button>();
        if (button == null)
            button = inventoryButtonRect.gameObject.AddComponent<Button>();

        button.targetGraphic = inventoryButtonImage;
        ConfigureButtonFrontLayer(button);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowRightPanelTab(RightPanelTab.Inventory));
        button.interactable = true;
    }

    private void ConfigureEnterShopButton()
    {
        enterShopButton = FindExistingCommandButton(enterShopButton, "enter");
        if (enterShopButton == null)
            return;

        if (enterShopButton.targetGraphic != null)
            enterShopButton.targetGraphic.raycastTarget = true;

        ConfigureButtonFrontLayer(enterShopButton);
        enterShopButton.onClick.RemoveAllListeners();
        enterShopButton.onClick.AddListener(() => gameManager?.InvokeMarketShopButton());
        enterShopButton.interactable = true;
    }

    private void EnsureRightPanelTabs()
    {
        if (rightUiBlockImage == null)
            return;

        if (rightPanelTabsRoot == null)
            rightPanelTabsRoot = rightUiBlockImage.transform.Find("Right Panel Tabs Root")?.GetComponent<RectTransform>();

        if (rightPanelTabsRoot == null)
        {
            if (Application.isPlaying)
                return;

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
            if (Application.isPlaying)
                return;

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
        ConfigureRightPanelFrontLayer();

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

    private Button FindExistingCommandButton(Button currentButton, string namePart)
    {
        if (currentButton != null && currentButton.gameObject.activeSelf)
            return currentButton;

        return FindButtonByNamePart(contentRoot != null ? contentRoot.transform : transform, namePart);
    }

    private void ConfigureExistingTabButton(Button button, RightPanelTab tab)
    {
        if (!IsUsableUserButton(button))
            return;

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;

        ConfigureButtonFrontLayer(button);
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
            if (Application.isPlaying)
                return null;

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

        if (tab == RightPanelTab.Objectives)
        {
            FTUEManager.NotifyObjectivesOpened(
                objectivesTabContent != null ? objectivesTabContent.transform as RectTransform : null,
                knownRecipesTabButton != null ? knownRecipesTabButton.transform as RectTransform : null);
        }
        else if (tab == RightPanelTab.KnownRecipes)
        {
            FTUEManager.NotifyKnownRecipesOpened();
        }
    }

    private void RefreshRightPanelTabs()
    {
        SetTabContentActive(objectivesTabContent, activeRightPanelTab == RightPanelTab.Objectives);
        SetTabContentActive(inventoryTabContent, activeRightPanelTab == RightPanelTab.Inventory);
        SetTabContentActive(knownRecipesTabContent, activeRightPanelTab == RightPanelTab.KnownRecipes);

        ApplyTabButtonVisual(objectivesTabButton, activeRightPanelTab == RightPanelTab.Objectives);
        ApplyTabButtonVisual(inventoryTabButton, activeRightPanelTab == RightPanelTab.Inventory);
        ApplyTabButtonVisual(knownRecipesTabButton, activeRightPanelTab == RightPanelTab.KnownRecipes);

        if (activeRightPanelTab == RightPanelTab.Objectives)
            PopulateObjectivesTab();

        if (activeRightPanelTab == RightPanelTab.Inventory)
            PopulateInventoryTab();

        if (activeRightPanelTab == RightPanelTab.KnownRecipes)
            PopulateKnownRecipesTab();
    }

    private void PopulateObjectivesTab()
    {
        if (objectivesTabContent == null)
            return;

        Transform contentTransform = objectivesTabContent.transform;
        Transform generated = contentTransform.Find(GeneratedObjectiveContentName);
        if (generated == null)
        {
            if (Application.isPlaying)
                return;

            generated = EnsureStretchChild(contentTransform, GeneratedObjectiveContentName);
        }

        SetChildrenActive(generated, false);

        ObjectiveManager objectiveManager = gameManager != null ? gameManager.objectiveManager : null;
        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>(FindObjectsInactive.Include);

        if (objectiveManager == null || objectiveManager.objectives == null || objectiveManager.objectives.Count == 0)
        {
            SetPanelText(generated, "Empty State Text", "No objectives yet", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
            return;
        }

        if (Application.isPlaying && gameManager != null)
            objectiveManager.UpdateTasksFromInventory(gameManager.GetInventoryItems());

        Objective objective = objectiveManager.objectives[0];
        float y = -30f;

        SetPanelText(
            generated,
            "Objective Title Text",
            $"Ritual Order: {objective.potionDisplayName}",
            new Vector2(0f, y),
            new Vector2(640f, 70f),
            32f,
            TextAlignmentOptions.Center);

        y -= 82f;
        SetPanelText(
            generated,
            "Ingredients Header Text",
            "Required Relics",
            new Vector2(-245f, y),
            new Vector2(260f, 44f),
            24f,
            TextAlignmentOptions.Left);

        y -= 48f;
        for (int i = 0; i < objective.ingredients.Count; i++)
        {
            bool discovered = objective.discovered != null &&
                i < objective.discovered.Count &&
                objective.discovered[i];

            string ingredientText = discovered ? objective.ingredients[i] : "???";
            SetPanelText(
                generated,
                $"Ingredient Row {i + 1}",
                $"> {ingredientText}",
                new Vector2(-210f, y),
                new Vector2(520f, 38f),
                21f,
                TextAlignmentOptions.Left);
            y -= 38f;
        }

        if (!objectiveManager.ShouldShowPreparations())
            return;

        y -= 20f;
        SetPanelText(
            generated,
            "Tasks Header Text",
            "Preparations",
            new Vector2(-245f, y),
            new Vector2(260f, 44f),
            24f,
            TextAlignmentOptions.Left);

        y -= 48f;
        for (int i = 0; i < objective.missions.Count; i++)
        {
            Mission mission = objective.missions[i];
            string progress = mission.type == MissionType.BuyItems
                ? (mission.completed ? " 1/1" : " 0/1")
                : string.Empty;
            string status = mission.completed ? "[done] " : "[ ] ";

            TMP_Text missionText = SetPanelText(
                generated,
                $"Mission Row {i + 1}",
                $"{status}{mission.missionText}{progress}",
                new Vector2(-195f, y),
                new Vector2(560f, 42f),
                20f,
                TextAlignmentOptions.Left);
            if (missionText != null)
                missionText.alpha = mission.completed ? 0.62f : 1f;
            y -= 42f;
        }
    }

    private void PopulateInventoryTab()
    {
        if (inventoryTabContent == null)
            return;

        Transform generated = inventoryTabContent.transform.Find(GeneratedInventoryContentName);
        if (generated == null)
        {
            if (Application.isPlaying)
                return;

            generated = EnsureStretchChild(inventoryTabContent.transform, GeneratedInventoryContentName);
        }

        RectTransform scrollContent = EnsureInventoryScrollContent(generated, !Application.isPlaying);
        if (scrollContent == null)
            return;

        SetChildrenActive(scrollContent, false);
        SetNamedChildActive(generated, "Inventory Empty State Text", false);

        List<InventoryItem> inventory = gameManager != null ? gameManager.GetInventoryItems() : null;
        if (inventory == null || inventory.Count == 0)
        {
            SetNamedChildActive(generated, InventoryScrollViewName, false);
            SetInventoryPanelText(generated, "Inventory Empty State Text", "Inventory is empty", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
            return;
        }

        SetNamedChildActive(generated, InventoryScrollViewName, true);

        ObjectiveManager objectiveManager = gameManager != null ? gameManager.objectiveManager : null;
        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>(FindObjectsInactive.Include);

        int visibleIndex = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            InventoryItem item = inventory[i];
            if (item == null || item.count <= 0)
                continue;

            visibleIndex++;
            Transform row = scrollContent.Find($"Inventory Row {visibleIndex}");
            if (row == null)
            {
                if (Application.isPlaying)
                    continue;

                row = EnsureInventoryRow(scrollContent, visibleIndex);
            }

            row.gameObject.SetActive(true);

            Image icon = row.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = item.icon;
                icon.enabled = item.icon != null;
            }

            TMP_Text nameText = row.Find("Name Text")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = item.itemName;

            TMP_Text countText = row.Find("Count Text")?.GetComponent<TMP_Text>();
            if (countText != null)
                countText.text = "x" + item.count;

            Button investigateButton = row.Find("Investigate Button")?.GetComponent<Button>();
            FillInvestigateButton fillButton = investigateButton != null ? investigateButton.GetComponent<FillInvestigateButton>() : null;
            if (investigateButton != null && fillButton != null)
            {
                bool canInvestigate = objectiveManager != null &&
                    objectiveManager.CanInvestigateToday() &&
                    objectiveManager.CanAffordInvestigation();

                fillButton.itemName = item.itemName;
                investigateButton.interactable = canInvestigate;
            }
        }

        UpdateInventoryScrollContentHeight(scrollContent, visibleIndex);

        if (visibleIndex == 0)
        {
            SetNamedChildActive(generated, InventoryScrollViewName, false);
            SetInventoryPanelText(generated, "Inventory Empty State Text", "Inventory is empty", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
        }
    }

    private void EnsureInventoryEditablePreview()
    {
        if (Application.isPlaying || inventoryTabContent == null)
            return;

        Transform generated = EnsureStretchChild(inventoryTabContent.transform, GeneratedInventoryContentName);
        RectTransform scrollContent = EnsureInventoryScrollContent(generated, true);
        if (scrollContent == null)
            return;

        int rowCount = Mathf.Max(1, editableInventoryRowCount);
        for (int i = 1; i <= rowCount; i++)
        {
            Transform row = EnsureInventoryRow(scrollContent, i);
            row.gameObject.SetActive(true);
        }

        UpdateInventoryScrollContentHeight(scrollContent, rowCount);
        SetNamedChildActive(generated, InventoryScrollViewName, true);
        SetInventoryPanelText(generated, "Inventory Empty State Text", "Inventory is empty", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
    }

    private RectTransform EnsureInventoryScrollContent(Transform parent, bool createIfMissing)
    {
        Transform scrollView = parent.Find(InventoryScrollViewName);
        if (scrollView == null)
        {
            if (!createIfMissing)
                return null;

            GameObject scrollObject = CreateRect(
                InventoryScrollViewName,
                parent,
                new Vector2(0f, -40f),
                new Vector2(660f, 560f));
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            SetTopCenteredRect(scrollRectTransform);

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;

            GameObject viewportObject = CreateRect(InventoryScrollViewportName, scrollObject.transform, Vector2.zero, Vector2.zero);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportObject.AddComponent<RectMask2D>();
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = CreateRect(InventoryScrollContentName, viewportObject.transform, Vector2.zero, Vector2.zero);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 560f);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        ScrollRect existingScrollRect = scrollView.GetComponent<ScrollRect>();
        Transform viewportTransform = scrollView.Find(InventoryScrollViewportName);
        Transform contentTransform = viewportTransform != null ? viewportTransform.Find(InventoryScrollContentName) : null;
        RectTransform existingContent = contentTransform as RectTransform;

        if (existingScrollRect != null && existingContent != null)
        {
            existingScrollRect.horizontal = false;
            existingScrollRect.vertical = true;
            existingScrollRect.movementType = ScrollRect.MovementType.Clamped;
            existingScrollRect.scrollSensitivity = 35f;
            existingScrollRect.content = existingContent;
            existingScrollRect.viewport = viewportTransform as RectTransform;
        }

        return existingContent;
    }

    private static void UpdateInventoryScrollContentHeight(RectTransform content, int visibleRowCount)
    {
        if (content == null)
            return;

        RectTransform viewport = content.parent as RectTransform;
        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        float contentHeight = Mathf.Max(viewportHeight, 28f + (Mathf.Max(visibleRowCount, 1) * InventoryRowSpacing));
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
    }

    private TMP_Text SetInventoryPanelText(
        Transform parent,
        string objectName,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(objectName);
        if (Application.isPlaying && existing == null)
            return null;

        return SetPanelText(parent, objectName, value, position, size, fontSize, alignment);
    }

    private static void SetNamedChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        if (child != null && child.gameObject.activeSelf != active)
            child.gameObject.SetActive(active);
    }

    private Transform EnsureInventoryRow(Transform parent, int rowIndex)
    {
        string rowName = $"Inventory Row {rowIndex}";
        Transform existing = parent.Find(rowName);
        if (existing != null)
            return existing;

        GameObject row = CreateRect(
            rowName,
            parent,
            new Vector2(0f, -38f - ((rowIndex - 1) * 86f)),
            new Vector2(640f, 78f));

        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetTopCenteredRect(rowRect);

        Image icon = CreateImage("Icon", row.transform, null);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchoredPosition = new Vector2(-270f, 0f);
        iconRect.sizeDelta = new Vector2(58f, 58f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text nameText = CreateText(
            "Name Text",
            row.transform,
            string.Empty,
            new Vector2(-105f, 12f),
            new Vector2(300f, 34f),
            21f,
            TextAlignmentOptions.Left);
        nameText.color = new Color(0.23f, 0.08f, 0.045f, 0.95f);
        nameText.fontStyle = FontStyles.Bold;

        TMP_Text countText = CreateText(
            "Count Text",
            row.transform,
            string.Empty,
            new Vector2(130f, 12f),
            new Vector2(90f, 34f),
            21f,
            TextAlignmentOptions.Center);
        countText.color = new Color(0.23f, 0.08f, 0.045f, 0.95f);
        countText.fontStyle = FontStyles.Bold;

        GameObject buttonObject = CreateRect(
            "Investigate Button",
            row.transform,
            new Vector2(205f, -20f),
            new Vector2(62f, 62f));
        Image fill = buttonObject.AddComponent<Image>();
        fill.sprite = LoadUiSprite("FillCircle");
        fill.color = new Color(0.07066631f, 1f, 0f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = 2;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = fill;

        Image outline = CreateImage("Outline", buttonObject.transform, LoadUiSprite("CircleOutline"));
        outline.rectTransform.sizeDelta = new Vector2(62f, 62f);
        outline.color = Color.white;
        outline.raycastTarget = false;

        TMP_Text label = CreateText(
            "Investigate Label",
            buttonObject.transform,
            string.Empty,
            Vector2.zero,
            new Vector2(62f, 18f),
            18f,
            TextAlignmentOptions.Center);
        label.color = new Color(1f, 0.88f, 0.62f, 1f);
        label.fontStyle = FontStyles.Bold;

        FillInvestigateButton fillButton = buttonObject.AddComponent<FillInvestigateButton>();
        fillButton.button = button;
        fillButton.outLineImage = outline;
        fillButton.fillImage = fill;

        return row.transform;
    }

    private void PopulateKnownRecipesTab()
    {
        if (knownRecipesTabContent == null)
            return;

        Transform generated = knownRecipesTabContent.transform.Find(GeneratedKnownRecipesContentName);
        if (generated == null)
        {
            if (Application.isPlaying)
                return;

            generated = EnsureStretchChild(knownRecipesTabContent.transform, GeneratedKnownRecipesContentName);
        }

        RectTransform scrollContent = EnsureKnownRecipesScrollContent(generated, !Application.isPlaying);
        if (scrollContent == null)
            return;

        SetChildrenActive(scrollContent, false);
        SetNamedChildActive(generated, "Known Recipes Empty State Text", false);

        List<Recipe> recipes = gameManager != null ? gameManager.recipes : null;
        if (recipes == null || recipes.Count == 0)
        {
            SetNamedChildActive(generated, KnownRecipesScrollViewName, false);
            SetKnownRecipesPanelText(generated, "Known Recipes Empty State Text", "No recipes yet", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
            return;
        }

        SetNamedChildActive(generated, KnownRecipesScrollViewName, true);

        int visibleIndex = 0;
        for (int i = 0; i < recipes.Count; i++)
        {
            Recipe recipe = recipes[i];
            if (recipe == null)
                continue;

            visibleIndex++;
            Transform card = scrollContent.Find($"Known Recipe Card {visibleIndex}");
            if (card == null)
            {
                if (Application.isPlaying)
                    continue;

                card = EnsureKnownRecipeCard(scrollContent, visibleIndex);
            }

            card.gameObject.SetActive(true);
            RefreshKnownRecipeCard(card, recipe);
        }

        UpdateKnownRecipesScrollContentHeight(scrollContent, visibleIndex);

        if (visibleIndex == 0)
        {
            SetNamedChildActive(generated, KnownRecipesScrollViewName, false);
            SetKnownRecipesPanelText(generated, "Known Recipes Empty State Text", "No recipes yet", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
        }
    }

    private void EnsureKnownRecipesEditablePreview()
    {
        if (Application.isPlaying || knownRecipesTabContent == null)
            return;

        Transform generated = EnsureStretchChild(knownRecipesTabContent.transform, GeneratedKnownRecipesContentName);
        RectTransform scrollContent = EnsureKnownRecipesScrollContent(generated, true);
        if (scrollContent == null)
            return;

        int cardCount = Mathf.Max(1, editableKnownRecipeCardCount);
        for (int i = 1; i <= cardCount; i++)
        {
            Transform card = EnsureKnownRecipeCard(scrollContent, i);
            card.gameObject.SetActive(true);
        }

        UpdateKnownRecipesScrollContentHeight(scrollContent, cardCount);
        SetNamedChildActive(generated, KnownRecipesScrollViewName, true);
        SetKnownRecipesPanelText(generated, "Known Recipes Empty State Text", "No recipes yet", new Vector2(0f, 80f), new Vector2(620f, 80f), 32f, TextAlignmentOptions.Center);
    }

    private RectTransform EnsureKnownRecipesScrollContent(Transform parent, bool createIfMissing)
    {
        Transform scrollView = parent.Find(KnownRecipesScrollViewName);
        if (scrollView == null)
        {
            if (!createIfMissing)
                return null;

            GameObject scrollObject = CreateRect(
                KnownRecipesScrollViewName,
                parent,
                new Vector2(0f, -40f),
                new Vector2(660f, 560f));
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            SetTopCenteredRect(scrollRectTransform);

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;

            GameObject viewportObject = CreateRect(KnownRecipesScrollViewportName, scrollObject.transform, Vector2.zero, Vector2.zero);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportObject.AddComponent<RectMask2D>();
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = CreateRect(KnownRecipesScrollContentName, viewportObject.transform, Vector2.zero, Vector2.zero);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 560f);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        ScrollRect existingScrollRect = scrollView.GetComponent<ScrollRect>();
        Transform viewportTransform = scrollView.Find(KnownRecipesScrollViewportName);
        Transform contentTransform = viewportTransform != null ? viewportTransform.Find(KnownRecipesScrollContentName) : null;
        RectTransform existingContent = contentTransform as RectTransform;

        if (existingScrollRect != null && existingContent != null)
        {
            existingScrollRect.horizontal = false;
            existingScrollRect.vertical = true;
            existingScrollRect.movementType = ScrollRect.MovementType.Clamped;
            existingScrollRect.scrollSensitivity = 35f;
            existingScrollRect.content = existingContent;
            existingScrollRect.viewport = viewportTransform as RectTransform;
        }

        return existingContent;
    }

    private Transform EnsureKnownRecipeCard(Transform parent, int cardIndex)
    {
        string cardName = $"Known Recipe Card {cardIndex}";
        Transform existing = parent.Find(cardName);
        if (existing != null)
            return existing;

        GameObject card = CreateRect(
            cardName,
            parent,
            new Vector2(0f, -95f - ((cardIndex - 1) * KnownRecipeCardSpacing)),
            new Vector2(620f, 205f));

        RectTransform cardRect = card.GetComponent<RectTransform>();
        SetTopCenteredRect(cardRect);

        Image background = card.AddComponent<Image>();
        background.color = new Color(0.12f, 0.08f, 0.04f, 0.16f);
        background.raycastTarget = false;

        Image resultIcon = CreateImage("ResultIcon", card.transform, null);
        RectTransform resultIconRect = resultIcon.rectTransform;
        resultIconRect.anchoredPosition = new Vector2(-225f, 38f);
        resultIconRect.sizeDelta = new Vector2(84f, 74f);
        resultIcon.preserveAspect = true;
        resultIcon.raycastTarget = false;

        TMP_Text unknownProduct = CreateText(
            "UnknownProduct",
            resultIcon.transform,
            "?",
            Vector2.zero,
            resultIconRect.sizeDelta,
            46f,
            TextAlignmentOptions.Center);
        unknownProduct.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        TMP_Text recipeName = CreateText(
            "RecipeName",
            card.transform,
            string.Empty,
            new Vector2(0f, 38f),
            new Vector2(340f, 68f),
            22f,
            TextAlignmentOptions.Center);
        recipeName.color = new Color(0.08f, 0.06f, 0.04f, 1f);
        recipeName.fontStyle = FontStyles.Bold;

        GameObject ingredientsRow = CreateRect(
            "IngredientsRow",
            card.transform,
            new Vector2(0f, -52f),
            new Vector2(380f, 58f));

        for (int i = 0; i < 3; i++)
            EnsureKnownRecipeIngredientSlot(ingredientsRow.transform, i);

        return card.transform;
    }

    private Transform EnsureKnownRecipeIngredientSlot(Transform parent, int ingredientIndex)
    {
        string slotName = $"Ingredient{ingredientIndex + 1}";
        Transform existing = parent.Find(slotName);
        if (existing != null)
            return existing;

        GameObject slot = CreateRect(
            slotName,
            parent,
            new Vector2(-114f + (ingredientIndex * 114f), 0f),
            new Vector2(76f, 58f));

        Image slotImage = slot.AddComponent<Image>();
        slotImage.raycastTarget = false;
        slotImage.preserveAspect = true;
        slotImage.color = Color.gray;

        TMP_Text unknownIngredient = CreateText(
            "UnknownIngredient",
            slot.transform,
            "?",
            Vector2.zero,
            slotImage.rectTransform.sizeDelta,
            25f,
            TextAlignmentOptions.Center);
        unknownIngredient.color = Color.white;
        unknownIngredient.fontStyle = FontStyles.Bold;

        return slot.transform;
    }

    private void RefreshKnownRecipeCard(Transform card, Recipe recipe)
    {
        bool recipeDiscovered = gameManager != null && gameManager.IsRecipeDiscovered(recipe);
        DisableOldGlow(card.GetComponent<Image>());

        Image resultIcon = card.Find("ResultIcon")?.GetComponent<Image>();
        if (resultIcon != null)
        {
            resultIcon.sprite = recipe.icon;
            resultIcon.color = recipeDiscovered ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
            resultIcon.enabled = recipe.icon != null;
            resultIcon.preserveAspect = true;
            ApplyUltimatePotionGlow(resultIcon, recipe.icon != null ? recipe : null);
        }

        SetNamedChildActive(card.Find("ResultIcon"), "UnknownProduct", false);

        TMP_Text recipeName = card.Find("RecipeName")?.GetComponent<TMP_Text>();
        if (recipeName != null)
        {
            recipeName.text = recipe.potionName;
        }

        Transform ingredientsRow = card.Find("IngredientsRow");
        if (ingredientsRow == null)
            return;

        for (int i = 0; i < 3; i++)
        {
            Transform slot = ingredientsRow.Find($"Ingredient{i + 1}");
            if (slot == null)
                continue;

            bool hasIngredient = recipe.ingredients != null && i < recipe.ingredients.Count;
            slot.gameObject.SetActive(hasIngredient);
            if (!hasIngredient)
                continue;

            string ingredientName = recipe.ingredients[i];
            bool ingredientDiscovered = gameManager != null && gameManager.IsRecipeIngredientSlotDiscovered(recipe, i);
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.sprite = ingredientDiscovered && gameManager != null
                    ? gameManager.GetKnownRecipeIngredientIcon(ingredientName)
                    : GetKnownRecipeIngredientFrameSprite(ingredientName);
                slotImage.color = slotImage.sprite != null ? Color.white : GetKnownRecipeIngredientColor(ingredientName);
                slotImage.preserveAspect = slotImage.sprite != null;
            }

            SetNamedChildActive(slot, "UnknownIngredient", !ingredientDiscovered);
        }
    }

    private void ApplyUltimatePotionGlow(Graphic targetGraphic, Recipe recipe)
    {
        if (targetGraphic == null)
            return;

        Outline glow = targetGraphic.GetComponent<Outline>();
        if (glow != null)
            glow.enabled = false;

        UltimatePotionAuraUtility.Apply(
            targetGraphic as Image,
            gameManager != null && gameManager.ShouldHighlightUltimatePotionRecipe(recipe),
            gameManager != null ? gameManager.GetUltimatePotionGlowColor() : Color.red,
            gameManager != null ? gameManager.GetUltimatePotionGlowIntensity() : 2.5f,
            gameManager != null ? gameManager.GetUltimatePotionGlowSpread() : 7f);
    }

    private static void DisableOldGlow(Graphic targetGraphic)
    {
        if (targetGraphic == null)
            return;

        Outline glow = targetGraphic.GetComponent<Outline>();
        if (glow != null)
            glow.enabled = false;
    }

    private Sprite GetKnownRecipeIngredientFrameSprite(string ingredientName)
    {
        if (!TryGetKnownRecipeIngredientCategory(ingredientName, out ItemCategory category))
            return null;

        switch (category)
        {
            case ItemCategory.Oils:
                return LoadSprite("Flower Frame");
            case ItemCategory.Herbs:
                return LoadSprite("seller");
            case ItemCategory.Gems:
                return LoadSprite("Crystal Frame");
            case ItemCategory.Potion:
                return LoadSprite("MaterialSlot");
            default:
                return null;
        }
    }

    private Color GetKnownRecipeIngredientColor(string ingredientName)
    {
        if (TryGetKnownRecipeIngredientCategory(ingredientName, out ItemCategory category))
        {
            switch (category)
            {
                case ItemCategory.Oils:
                    return new Color(0.28f, 0.17f, 0.05f, 1f);
                case ItemCategory.Herbs:
                    return new Color(0.12f, 0.72f, 0.32f, 1f);
                case ItemCategory.Gems:
                    return new Color(0.35f, 0.16f, 0.82f, 1f);
                case ItemCategory.Potion:
                    return new Color(0.8f, 0.12f, 0.12f, 1f);
            }
        }

        return Color.gray;
    }

    private bool TryGetKnownRecipeIngredientCategory(string ingredientName, out ItemCategory category)
    {
        category = default;
        string normalizedIngredient = NormalizeLocalName(ingredientName);

        if (gameManager != null && gameManager.markets != null)
        {
            foreach (Market market in gameManager.markets)
            {
                if (market == null || market.items == null)
                    continue;

                foreach (MarketItem item in market.items)
                {
                    if (item == null || NormalizeLocalName(item.itemName) != normalizedIngredient)
                        continue;

                    category = item.category;
                    return true;
                }
            }
        }

        if (gameManager != null && gameManager.recipes != null)
        {
            foreach (Recipe recipe in gameManager.recipes)
            {
                if (recipe == null || NormalizeLocalName(recipe.potionName) != normalizedIngredient)
                    continue;

                category = recipe.category;
                return true;
            }
        }

        return false;
    }

    private static void UpdateKnownRecipesScrollContentHeight(RectTransform content, int visibleCardCount)
    {
        if (content == null)
            return;

        RectTransform viewport = content.parent as RectTransform;
        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        float contentHeight = Mathf.Max(viewportHeight, 32f + (Mathf.Max(visibleCardCount, 1) * KnownRecipeCardSpacing));
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
    }

    private TMP_Text SetKnownRecipesPanelText(
        Transform parent,
        string objectName,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(objectName);
        if (Application.isPlaying && existing == null)
            return null;

        return SetPanelText(parent, objectName, value, position, size, fontSize, alignment);
    }

    private static string NormalizeLocalName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private Transform EnsureStretchChild(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
            return existing;

        GameObject obj = CreateRect(objectName, parent, Vector2.zero, Vector2.zero);
        Stretch(obj.GetComponent<RectTransform>());
        return obj.transform;
    }

    private TMP_Text SetPanelText(
        Transform parent,
        string objectName,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        TMP_Text text = parent.Find(objectName)?.GetComponent<TMP_Text>();
        bool createdText = false;
        if (text == null)
        {
            if (Application.isPlaying)
                return null;

            text = CreateText(objectName, parent, value, position, size, fontSize, alignment);
            createdText = true;
        }
        else
        {
            text.text = value;
        }

        text.gameObject.SetActive(true);
        if (createdText)
        {
            text.color = new Color(0.23f, 0.08f, 0.045f, 0.95f);
            text.fontStyle = FontStyles.Bold;
        }

        AtmosphericObjectiveTextStyler.Apply(text, objectName);
        return text;
    }

    private static void SetChildrenActive(Transform parent, bool active)
    {
        if (parent == null)
            return;

        for (int i = 0; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(active);
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

        // Tab selection changes panel visibility only. Button colors are owned by the scene layout.
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

        EnsureRightPanelTabs();

        FamilyPage page = pages[pageIndex];

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
    }

    private void ApplyCharacterVisibility(string characterTexture)
    {
        DisableGeneratedFamilyMembers();
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

        ResolveFamilyArrows(contentTransform);

        ConfigureArrowFrontLayer(leftArrowRect);
        ConfigureArrowFrontLayer(rightArrowRect);

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

    private void ResolveFamilyArrows(Transform contentTransform)
    {
        List<RectTransform> arrows = new List<RectTransform>();

        AddFamilyArrow(arrows, leftArrowRect);
        AddFamilyArrow(arrows, rightArrowRect);

        for (int i = 0; i < contentTransform.childCount; i++)
        {
            Transform child = contentTransform.GetChild(i);
            if (child is RectTransform rect)
                AddFamilyArrow(arrows, rect);
        }

        if (arrows.Count == 0)
            return;

        if (arrows.Count == 1)
        {
            RectTransform onlyArrow = arrows[0];
            if (onlyArrow.localScale.x < 0f || onlyArrow.anchoredPosition.x < 0f)
                leftArrowRect = onlyArrow;
            else
                rightArrowRect = onlyArrow;
            return;
        }

        RectTransform leftCandidate = arrows[0];
        RectTransform rightCandidate = arrows[0];

        for (int i = 1; i < arrows.Count; i++)
        {
            RectTransform arrow = arrows[i];
            if (arrow.anchoredPosition.x < leftCandidate.anchoredPosition.x)
                leftCandidate = arrow;

            if (arrow.anchoredPosition.x > rightCandidate.anchoredPosition.x)
                rightCandidate = arrow;
        }

        if (leftCandidate != rightCandidate)
        {
            leftArrowRect = leftCandidate;
            rightArrowRect = rightCandidate;
        }
    }

    private static void AddFamilyArrow(List<RectTransform> arrows, RectTransform arrow)
    {
        if (arrow == null || !IsFamilyArrowName(arrow.name) || arrows.Contains(arrow))
            return;

        arrows.Add(arrow);
    }

    private static bool IsFamilyArrowName(string objectName)
    {
        return objectName.StartsWith("Family Arrow");
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


    private void ConfigureDeskFrontLayer(GameObject deskObject)
    {
        ConfigureFrontUiLayer(deskObject, 110);
    }

    private void ConfigureArrowFrontLayer(RectTransform arrowRect)
    {
        if (arrowRect == null)
            return;

        ConfigureFrontUiLayer(arrowRect.gameObject, 140);
        arrowRect.SetAsLastSibling();
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

        if (uiObject.GetComponent<GraphicRaycaster>() == null)
            uiObject.AddComponent<GraphicRaycaster>();
    }

    private void ConfigureRightPanelFrontLayer()
    {
        if (rightUiBlockImage == null)
            return;

        rightUiBlockImage.raycastTarget = false;
        ConfigureFrontUiLayer(rightUiBlockImage.gameObject, RightPanelSortingOrder);

        if (rightPanelTabsRoot != null)
            rightPanelTabsRoot.SetAsLastSibling();
    }

    private void ConfigureButtonFrontLayer(Button button)
    {
        if (button == null)
            return;

        Canvas canvas = button.GetComponent<Canvas>();
        if (canvas == null)
            canvas = button.gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = RightPanelButtonSortingOrder;

        if (button.GetComponent<GraphicRaycaster>() == null)
            button.gameObject.AddComponent<GraphicRaycaster>();

        Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == button.targetGraphic)
                graphic.raycastTarget = true;
            else if (graphic.GetComponent<Button>() == null)
                graphic.raycastTarget = false;
        }
    }

    private void ApplyRightUiBlockLayout()
    {
        if (rightUiBlockImage == null || gameManager == null)
            return;

        RectTransform rect = rightUiBlockImage.rectTransform;
        SetCenteredRect(rect);
    }

    private void ApplyInventoryButtonLayout()
    {
        if (inventoryButtonRect == null || gameManager == null)
            return;

        SetCenteredRect(inventoryButtonRect);
        inventoryButtonRect.SetAsLastSibling();
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

    private Sprite LoadUiSprite(string spriteName)
    {
        if (spriteCache.TryGetValue(spriteName, out Sprite sprite))
            return sprite;

        sprite = Resources.Load<Sprite>(spriteName);

#if UNITY_EDITOR
        if (sprite == null)
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI Assets/{spriteName}.png");
#endif

        if (sprite != null)
            spriteCache.Add(spriteName, sprite);

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

public static class UltimatePotionAuraUtility
{
    private const string AuraObjectName = "Ultimate Potion Shader Aura";
    private const string ShaderName = "VoodooStore/UI Alpha Aura Glow";
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int GlowSpreadId = Shader.PropertyToID("_GlowSpread");

    public static void Apply(Image sourceImage, bool shouldGlow, Color glowColor, float intensity, float spread)
    {
        if (sourceImage == null)
            return;

        Outline oldOutline = sourceImage.GetComponent<Outline>();
        if (oldOutline != null)
            oldOutline.enabled = false;

        Image auraImage = GetOrCreateAuraImage(sourceImage);
        if (auraImage == null)
            return;

        if (!shouldGlow || sourceImage.sprite == null)
        {
            auraImage.gameObject.SetActive(false);
            return;
        }

        auraImage.gameObject.SetActive(true);
        auraImage.sprite = sourceImage.sprite;
        auraImage.preserveAspect = sourceImage.preserveAspect;
        auraImage.raycastTarget = false;
        auraImage.color = Color.white;

        RectTransform sourceRect = sourceImage.rectTransform;
        RectTransform auraRect = auraImage.rectTransform;
        auraRect.anchorMin = new Vector2(0.5f, 0.5f);
        auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot = new Vector2(0.5f, 0.5f);
        auraRect.anchoredPosition = Vector2.zero;
        auraRect.localRotation = Quaternion.identity;
        auraRect.localScale = Vector3.one;
        auraRect.sizeDelta = sourceRect.rect.size + Vector2.one * Mathf.Max(0f, spread * 4f);
        auraRect.SetAsFirstSibling();

        Material material = GetOrCreateAuraMaterial(auraImage);
        if (material == null)
            return;

        material.SetColor(GlowColorId, glowColor);
        material.SetFloat(GlowIntensityId, Mathf.Max(0f, intensity));
        material.SetFloat(GlowSpreadId, Mathf.Max(0f, spread));
    }

    private static Image GetOrCreateAuraImage(Image sourceImage)
    {
        Transform existing = sourceImage.transform.Find(AuraObjectName);
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject auraObject = new GameObject(AuraObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        auraObject.transform.SetParent(sourceImage.transform, false);
        return auraObject.GetComponent<Image>();
    }

    private static Material GetOrCreateAuraMaterial(Image auraImage)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
            return null;

        if (auraImage.material != null && auraImage.material.shader == shader)
            return auraImage.material;

        Material material = new Material(shader)
        {
            name = "Generated Ultimate Potion Aura Material",
            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };
        auraImage.material = material;
        return material;
    }
}

public static class AtmosphericObjectiveTextStyler
{
    private const int StyleVersion = 1;

    public static void Apply(TMP_Text text, string objectName)
    {
        if (text == null || string.IsNullOrWhiteSpace(objectName))
            return;

        ObjectiveTextStyleMarker marker = text.GetComponent<ObjectiveTextStyleMarker>();
        if (marker != null && marker.version >= StyleVersion)
            return;

        if (marker == null)
            marker = text.gameObject.AddComponent<ObjectiveTextStyleMarker>();

        marker.version = StyleVersion;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        if (objectName.Contains("Title"))
            ApplyTitleStyle(text);
        else if (objectName.Contains("Header"))
            ApplyHeaderStyle(text);
        else if (objectName.Contains("Ingredient"))
            ApplyIngredientStyle(text);
        else if (objectName.Contains("Mission"))
            ApplyMissionStyle(text);
        else if (objectName.Contains("Empty"))
            ApplyEmptyStyle(text);
    }

    private static void ApplyTitleStyle(TMP_Text text)
    {
        text.fontSize = 29f;
        text.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.98f, 0.78f, 0.42f, 1f);
        AddShadow(text, new Color(0.08f, 0.01f, 0.01f, 0.85f), new Vector2(2f, -3f));
        AddOutline(text, new Color(0.18f, 0.02f, 0.02f, 0.88f), new Vector2(1.4f, -1.4f));
    }

    private static void ApplyHeaderStyle(TMP_Text text)
    {
        text.fontSize = 23f;
        text.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(0.86f, 0.26f, 0.16f, 1f);
        AddShadow(text, new Color(0.05f, 0f, 0f, 0.72f), new Vector2(1.5f, -2f));
        AddOutline(text, new Color(0.95f, 0.62f, 0.25f, 0.35f), new Vector2(0.8f, -0.8f));
    }

    private static void ApplyIngredientStyle(TMP_Text text)
    {
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(0.95f, 0.84f, 0.62f, 1f);
        AddShadow(text, new Color(0.08f, 0.01f, 0.01f, 0.76f), new Vector2(1f, -1.5f));
    }

    private static void ApplyMissionStyle(TMP_Text text)
    {
        text.fontSize = 19f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(0.82f, 0.76f, 0.66f, 1f);
        AddShadow(text, new Color(0.04f, 0f, 0f, 0.75f), new Vector2(1f, -1.5f));
    }

    private static void ApplyEmptyStyle(TMP_Text text)
    {
        text.fontSize = 29f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.9f, 0.75f, 0.5f, 1f);
        AddShadow(text, new Color(0.08f, 0.01f, 0.01f, 0.72f), new Vector2(2f, -2f));
    }

    private static void AddShadow(TMP_Text text, Color color, Vector2 distance)
    {
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddOutline(TMP_Text text, Color color, Vector2 distance)
    {
        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
            outline = text.gameObject.AddComponent<Outline>();

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }
}
