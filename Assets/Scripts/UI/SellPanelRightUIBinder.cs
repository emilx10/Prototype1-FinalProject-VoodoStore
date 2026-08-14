using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SellPanelRightUIBinder : MonoBehaviour
{
    private enum RightPanelTab
    {
        Objectives,
        Inventory,
        KnownRecipes
    }

    private const string GeneratedObjectiveContentName = "Generated Objectives Content";
    private const string GeneratedInventoryContentName = "Generated Inventory Content";
    private const string InventoryScrollContentName = "Inventory Scroll Content";
    private const string GeneratedKnownRecipesContentName = "Generated Known Recipes Content";
    private const string KnownRecipesScrollContentName = "Known Recipes Scroll Content";
    private const float InventoryRowSpacing = 86f;
    private const float KnownRecipeCardSpacing = 230f;

    private static readonly List<SellPanelRightUIBinder> instances = new List<SellPanelRightUIBinder>();

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private RightPanelTab activeTab = RightPanelTab.Inventory;
    [Tooltip("When enabled, existing text in the sell panel right UI block keeps your font size, color, alignment, placement, and style. Runtime only updates the text value.")]
    [SerializeField] private bool preserveRightPanelTextEdits = true;

    [Header("Exact Top Menu Buttons")]
    [SerializeField] private Button objectivesTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button knownRecipesPotionButton;

    private Button objectivesButton;
    private Button inventoryButton;
    private Button knownRecipesButton;
    private GameObject objectivesContent;
    private GameObject inventoryContent;
    private GameObject knownRecipesContent;

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);

        Bind();
        Refresh();
    }

    private void OnDisable()
    {
        instances.Remove(this);
    }

    public static void RefreshVisible()
    {
        for (int i = instances.Count - 1; i >= 0; i--)
        {
            SellPanelRightUIBinder binder = instances[i];
            if (binder == null)
            {
                instances.RemoveAt(i);
                continue;
            }

            if (binder.isActiveAndEnabled)
                binder.Refresh();
        }
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    private void Bind()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

        objectivesButton = objectivesTabButton != null ? objectivesTabButton : FindButtonByNamePart("objective");
        inventoryButton = inventoryTabButton != null ? inventoryTabButton : FindButtonByNamePart("inventory");
        knownRecipesButton = knownRecipesPotionButton != null ? knownRecipesPotionButton : FindButtonByNamePart("recipe");

        ConfigureButton(objectivesButton, RightPanelTab.Objectives);
        ConfigureButton(inventoryButton, RightPanelTab.Inventory);
        ConfigureButton(knownRecipesButton, RightPanelTab.KnownRecipes);

        Transform contentRoot = transform.Find("Right Panel Tabs Root/Right Panel Content Root");
        if (contentRoot != null)
        {
            objectivesContent = contentRoot.Find("Objectives Tab Content")?.gameObject;
            inventoryContent = contentRoot.Find("Inventory Tab Content")?.gameObject;
            knownRecipesContent = contentRoot.Find("Known Recipes Tab Content")?.gameObject;
        }
    }

    private Button FindButtonByNamePart(string namePart)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        string lowerNamePart = namePart.ToLowerInvariant();

        for (int i = 0; i < buttons.Length; i++)
        {
            string lowerName = buttons[i].gameObject.name.ToLowerInvariant();
            if (lowerName.Contains(lowerNamePart))
                return buttons[i];
        }

        return null;
    }

    private void ConfigureButton(Button button, RightPanelTab tab)
    {
        if (button == null)
            return;

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            activeTab = tab;
            Refresh();

            if (tab == RightPanelTab.Objectives)
            {
                FTUEManager.NotifyObjectivesOpened(
                    objectivesContent != null ? objectivesContent.transform as RectTransform : null,
                    knownRecipesButton != null ? knownRecipesButton.transform as RectTransform : null,
                    objectivesButton != null ? objectivesButton.GetComponent<ButtonBreather>() : null,
                    gameManager != null ? gameManager.MarketAttentionBreather : null);
            }
            else if (tab == RightPanelTab.KnownRecipes)
            {
                FTUEManager.NotifyKnownRecipesOpened(
                    knownRecipesContent != null ? knownRecipesContent.transform as RectTransform : null);
            }
        });
        button.interactable = true;
    }

    public void Refresh()
    {
        SetContentActive(objectivesContent, activeTab == RightPanelTab.Objectives);
        SetContentActive(inventoryContent, activeTab == RightPanelTab.Inventory);
        SetContentActive(knownRecipesContent, activeTab == RightPanelTab.KnownRecipes);

        ApplyButtonVisual(objectivesButton, activeTab == RightPanelTab.Objectives);
        ApplyButtonVisual(inventoryButton, activeTab == RightPanelTab.Inventory);
        ApplyButtonVisual(knownRecipesButton, activeTab == RightPanelTab.KnownRecipes);

        if (activeTab == RightPanelTab.Objectives)
            PopulateObjectives();
        else if (activeTab == RightPanelTab.Inventory)
            PopulateInventory();
        else if (activeTab == RightPanelTab.KnownRecipes)
            PopulateKnownRecipes();

        if (gameManager != null)
            gameManager.RefreshBrewButtonVisibility();

        RectTransform inventoryRect = inventoryContent != null ? inventoryContent.transform as RectTransform : null;
        if (activeTab == RightPanelTab.Inventory && inventoryContent != null && inventoryContent.activeInHierarchy)
            FTUEManager.NotifyInventoryOpened(inventoryRect);
        else
            FTUEManager.NotifyInventoryClosed(inventoryRect);
    }

    private void PopulateObjectives()
    {
        Transform generated = objectivesContent != null
            ? objectivesContent.transform.Find(GeneratedObjectiveContentName)
            : null;
        if (generated == null)
            return;

        YellowObjectiveTemplate.Apply(generated);
        SetChildrenActive(generated, false);

        ObjectiveManager objectiveManager = gameManager != null ? gameManager.objectiveManager : null;
        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>(FindObjectsInactive.Include);

        if (objectiveManager == null || objectiveManager.objectives == null || objectiveManager.objectives.Count == 0)
        {
            SetExistingText(generated, "Empty State Text", "No objectives yet");
            return;
        }

        if (Application.isPlaying && gameManager != null)
            objectiveManager.UpdateTasksFromInventory(gameManager.GetInventoryItems());

        if (objectiveManager.IsKnowledgeObjectiveActive)
        {
            SetExistingText(generated, "Objective Title Text", "Expand Your Knowledge");
            SetExistingText(generated, "Ingredients Header Text", "Requirements");
            string recipeStatus = objectiveManager.DiscoveredRecipeCount >= objectiveManager.KnowledgeRecipeGoal ? "[X]" : "[ ]";
            TMP_Text recipeText = SetExistingText(generated, "Mission Row 1", $"{recipeStatus} Discover recipes {objectiveManager.DiscoveredRecipeCount}/{objectiveManager.KnowledgeRecipeGoal}");
            string saleStatus = objectiveManager.SuccessfulProductSaleCount >= objectiveManager.KnowledgeProductSalesGoal ? "[X]" : "[ ]";
            TMP_Text saleText = SetExistingText(generated, "Mission Row 2", $"{saleStatus} Sell products {objectiveManager.SuccessfulProductSaleCount}/{objectiveManager.KnowledgeProductSalesGoal}");
            SetExistingText(generated, "Tasks Header Text", "Reward");
            SetExistingText(generated, "Ingredient Row 1", $"{objectiveManager.KnowledgeReward} Coins");

            if (recipeText != null)
                recipeText.alpha = recipeStatus == "[X]" ? 0.62f : 1f;
            if (saleText != null)
                saleText.alpha = saleStatus == "[X]" ? 0.62f : 1f;

            float titleY = YellowObjectiveTemplate.GetObjectiveTitleY(generated);
            PositionObjectiveText(generated, "Objective Title Text", new Vector2(-24f, titleY), new Vector2(640f, 72.05f));
            PositionObjectiveText(generated, "Ingredients Header Text", new Vector2(-162.5f, titleY - 82f), new Vector2(325f, 58f));
            PositionObjectiveText(generated, "Mission Row 1", new Vector2(-45f, titleY - 132f), new Vector2(560f, 43.23f));
            PositionObjectiveText(generated, "Mission Row 2", new Vector2(-45f, titleY - 172f), new Vector2(560f, 43.23f));
            PositionObjectiveText(generated, "Tasks Header Text", new Vector2(-195f, titleY - 234f), new Vector2(325f, 45.29f));
            PositionObjectiveText(generated, "Ingredient Row 1", new Vector2(-56f, titleY - 280f), new Vector2(520f, 39.12f));
            return;
        }

        Objective objective = objectiveManager.objectives[0];
        SetExistingText(generated, "Objective Title Text", $"Brew a {objective.potionDisplayName}");
        SetExistingText(generated, "Ingredients Header Text", "Required Ingredients");

        for (int i = 0; i < objective.ingredients.Count; i++)
        {
            bool discovered = objective.discovered != null &&
                i < objective.discovered.Count &&
                objective.discovered[i];
            SetExistingText(generated, $"Ingredient Row {i + 1}", "> " + (discovered ? objective.ingredients[i] : "???"));
        }

        if (!objectiveManager.ShouldShowPreparations())
            return;

        SetExistingText(generated, "Tasks Header Text", "Preparations");
        for (int i = 0; i < objective.missions.Count; i++)
        {
            Mission mission = objective.missions[i];
            string progress = mission.type == MissionType.BuyItems
                ? (mission.completed ? " 1/1" : " 0/1")
                : string.Empty;
            string status = mission.completed ? "[X] " : "[ ] ";
            TMP_Text missionText = SetExistingText(generated, $"Mission Row {i + 1}", $"{status}{mission.missionText}{progress}");
            if (missionText != null)
                missionText.alpha = mission.completed ? 0.62f : 1f;
        }

        LayoutObjectiveOne(generated, objective.ingredients.Count, objective.missions.Count);
    }

    private static void LayoutObjectiveOne(Transform generated, int ingredientCount, int missionCount)
    {
        const float objectiveOneContentOffset = 18f;
        float titleY = YellowObjectiveTemplate.GetObjectiveTitleY(generated);
        PositionObjectiveText(generated, "Objective Title Text", new Vector2(-24f, titleY), new Vector2(640f, 72.05f));

        float y = titleY - 82f - objectiveOneContentOffset;
        PositionObjectiveText(generated, "Ingredients Header Text", new Vector2(-162.5f, y), new Vector2(325f, 58f));
        y -= 50f;
        for (int i = 0; i < ingredientCount; i++)
        {
            PositionObjectiveText(generated, $"Ingredient Row {i + 1}", new Vector2(-56f, y), new Vector2(520f, 39.12f));
            y -= 38f;
        }

        y -= 12f;
        PositionObjectiveText(generated, "Tasks Header Text", new Vector2(-195f, y), new Vector2(325f, 45.29f));
        y -= 46f;
        for (int i = 0; i < missionCount; i++)
        {
            PositionObjectiveText(generated, $"Mission Row {i + 1}", new Vector2(-45f, y), new Vector2(560f, 43.23f));
            y -= 40f;
        }
    }

    private void PopulateInventory()
    {
        Transform scrollContent = FindDeepChild(inventoryContent != null ? inventoryContent.transform : null, InventoryScrollContentName);
        if (scrollContent == null)
            return;

        SetChildrenActive(scrollContent, false);

        List<InventoryItem> inventory = gameManager != null ? gameManager.GetInventoryItems() : null;
        if (inventory == null)
            return;

        ObjectiveManager objectiveManager = gameManager != null ? gameManager.objectiveManager : null;
        int visibleIndex = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            InventoryItem item = inventory[i];
            if (item == null || item.count <= 0)
                continue;

            visibleIndex++;
            Transform row = scrollContent.Find($"Inventory Row {visibleIndex}");
            if (row == null)
                continue;

            row.gameObject.SetActive(true);
            Image icon = row.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = item.icon;
                icon.enabled = item.icon != null;
            }

            SetExistingText(row, "Name Text", item.itemName);
            SetExistingText(row, "Count Text", "x" + item.count);

            Button investigateButton = row.Find("Investigate Button")?.GetComponent<Button>();
            FillInvestigateButton fillButton = investigateButton != null ? investigateButton.GetComponent<FillInvestigateButton>() : null;
            if (investigateButton != null && fillButton != null)
            {
                fillButton.itemName = item.itemName;
                investigateButton.interactable = objectiveManager != null &&
                    objectiveManager.CanInvestigateToday() &&
                    objectiveManager.CanAffordInvestigation();
            }
        }

        UpdateScrollHeight(scrollContent as RectTransform, visibleIndex, InventoryRowSpacing);
    }

    private void PopulateKnownRecipes()
    {
        Transform scrollContent = FindDeepChild(knownRecipesContent != null ? knownRecipesContent.transform : null, KnownRecipesScrollContentName);
        if (scrollContent == null || gameManager == null || gameManager.recipes == null)
            return;

        SetChildrenActive(scrollContent, false);

        int visibleIndex = 0;
        for (int i = 0; i < gameManager.recipes.Count; i++)
        {
            Recipe recipe = gameManager.recipes[i];
            if (recipe == null)
                continue;

            visibleIndex++;
            Transform card = scrollContent.Find($"Known Recipe Card {visibleIndex}");
            if (card == null)
                continue;

            card.gameObject.SetActive(true);
            RefreshRecipeCard(card, recipe);
        }

        UpdateScrollHeight(scrollContent as RectTransform, visibleIndex, KnownRecipeCardSpacing);
    }

    private void RefreshRecipeCard(Transform card, Recipe recipe)
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
        SetExistingText(card, "RecipeName", recipe.potionName);

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
            bool discovered = gameManager != null && gameManager.IsRecipeIngredientSlotDiscovered(recipe, i);
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.sprite = discovered && gameManager != null
                    ? gameManager.GetKnownRecipeIngredientIcon(ingredientName)
                    : GetKnownRecipeIngredientFrameSprite(ingredientName);
                slotImage.color = slotImage.sprite != null ? Color.white : GetKnownRecipeIngredientColor(ingredientName);
                slotImage.preserveAspect = slotImage.sprite != null;
            }

            SetNamedChildActive(slot, "UnknownIngredient", !discovered);
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
                return LoadFamilyMarketSprite("Flower Frame");
            case ItemCategory.Herbs:
                return LoadFamilyMarketSprite("seller");
            case ItemCategory.Gems:
                return LoadFamilyMarketSprite("Crystal Frame");
            case ItemCategory.Potion:
                return LoadFamilyMarketSprite("MaterialSlot");
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

        return new Color(0.62f, 0.62f, 0.62f, 1f);
    }

    private bool TryGetKnownRecipeIngredientCategory(string ingredientName, out ItemCategory category)
    {
        if (gameManager != null && gameManager.markets != null)
        {
            category = default;
            string normalizedIngredientName = NormalizeLocalName(ingredientName);

            for (int i = 0; i < gameManager.markets.Count; i++)
            {
                Market market = gameManager.markets[i];
                if (market == null || market.items == null)
                    continue;

                for (int itemIndex = 0; itemIndex < market.items.Count; itemIndex++)
                {
                    MarketItem item = market.items[itemIndex];
                    if (item == null || NormalizeLocalName(item.itemName) != normalizedIngredientName)
                        continue;

                    category = item.category;
                    return true;
                }
            }
        }

        if (gameManager != null && gameManager.recipes != null)
        {
            string normalizedIngredientName = NormalizeLocalName(ingredientName);

            for (int i = 0; i < gameManager.recipes.Count; i++)
            {
                Recipe recipe = gameManager.recipes[i];
                if (recipe == null || NormalizeLocalName(recipe.potionName) != normalizedIngredientName)
                    continue;

                category = recipe.category;
                return true;
            }
        }

        category = default;
        return false;
    }

    private Sprite LoadFamilyMarketSprite(string resourceName)
    {
        if (spriteCache.TryGetValue(resourceName, out Sprite sprite))
            return sprite;

        string resourcePath = $"FamilyMarket/{resourceName}";
        sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            spriteCache.Add(resourceName, sprite);
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = resourceName;
        spriteCache.Add(resourceName, sprite);
        return sprite;
    }

    private static string NormalizeLocalName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private TMP_Text SetExistingText(Transform parent, string objectName, string value)
    {
        TMP_Text text = parent != null ? parent.Find(objectName)?.GetComponent<TMP_Text>() : null;
        if (text == null)
            return null;

        text.text = value;
        text.gameObject.SetActive(true);
        if (!Application.isPlaying || !preserveRightPanelTextEdits)
            AtmosphericObjectiveTextStyler.Apply(text, objectName);
        return text;
    }

    private static void PositionObjectiveText(Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        RectTransform rect = parent != null ? parent.Find(objectName) as RectTransform : null;
        if (rect == null)
            return;

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetChildrenActive(Transform parent, bool active)
    {
        if (parent == null)
            return;

        for (int i = 0; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(active);
    }

    private static void SetNamedChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        if (child != null && child.gameObject.activeSelf != active)
            child.gameObject.SetActive(active);
    }

    private static void SetContentActive(GameObject content, bool active)
    {
        if (content != null && content.activeSelf != active)
            content.SetActive(active);
    }

    private static void ApplyButtonVisual(Button button, bool active)
    {
        if (button == null)
            return;

        // Tab selection changes panel visibility only. Button colors are owned by the scene layout.
    }

    private static void UpdateScrollHeight(RectTransform content, int visibleCount, float spacing)
    {
        if (content == null)
            return;

        RectTransform viewport = content.parent as RectTransform;
        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        float contentHeight = Mathf.Max(viewportHeight, 32f + (Mathf.Max(visibleCount, 1) * spacing));
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
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
}
