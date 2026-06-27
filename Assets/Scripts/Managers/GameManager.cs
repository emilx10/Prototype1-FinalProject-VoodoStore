using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ItemCategory
{
    Gems,
    Oils,
    Herbs,
    Potion,
    Junk
}


[System.Serializable]
public class MarketItem
{
    public string itemName;
    public int price;
    public ItemCategory category;

    [TextArea(2, 5)]
    public string description;

    [Header("Icon")]
    public Sprite icon;

    [Header("Sell Price Limits")]
    public int minSellPrice;
    public int maxSellPrice;

    [Header("Random Amount Between Min->Max")]
    public int minAmount;
    public int maxAmount;
}

[System.Serializable]
public class Market
{
    public string marketName;
    public List<MarketItem> items;
}

[System.Serializable]
public class Recipe
{
    public string potionName;
    public ItemCategory category;
    public List<string> ingredients;

    [Header("Icon")]
    public Sprite icon;

    [Header("Sell Price Limits")]
    public int minSellPrice;
    public int maxSellPrice;
}

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public int count;
    public ItemCategory category;
    public string description;
    public Sprite icon;
}

[System.Serializable]
public struct CategoryStyle
{
    public ItemCategory category;
    public Color backgroundColor;
    public Color textColor;
}

public class GameManager : MonoBehaviour
{
    private const int ShopItemsSortingOrder = 150;
    private const int KnownRecipeColumns = 3;
    private const float MergeDissolveStart = -2f;
    private const float MergeDissolveEnd = 1f;

    private HashSet<string> discoveredRecipes = new HashSet<string>();
    private HashSet<string> discoveredRecipeIngredientSlots = new HashSet<string>();

    [Header("SoundManager")]
    public AudioManager ad;
    [SerializeField] float vol,pitch;

    [Header("Category Styles")]
    [SerializeField] private List<CategoryStyle> categoryStyles;

    [Header("UI Panels")]
    public GameObject marketPanel;
    public GameObject itemsPanel;
    public GameObject craftingPanel;
    public GameObject sellPanel;

    [Header("TMP UI Elements")]
    public TMP_Text inventoryText;
    public TMP_Text coinsText;

    public Transform marketButtonsParent;
    public Transform itemsButtonsParent;
    public Transform craftingItemsParent;
    public Transform sellItemsParent;

    [Header("Crafting Triangle Layout")]
    [SerializeField] private Vector2 triangleLeftTop;
    [SerializeField] private Vector2 triangleRightTop;
    [SerializeField] private Vector2 triangleCenterBottom;

    [Header("Junk Settings")]
    [SerializeField] private string junkItemName = "Junk";
    [SerializeField] private int junkSellPrice = 1;
    [SerializeField] private Sprite junkIcon;

    [Header("Crafting Selection UI")]
    public Transform selectedItemsParent;
    public GameObject selectedItemTextPrefab;
    [SerializeField] private Button endDayButton;
    [SerializeField] private Button marketShopButton;

    public GameObject buttonPrefab;

    [Header("Game Data")]
    public List<Market> markets;
    public List<Recipe> recipes;
    public int coins = 100;

    [Header("Sell Confirmation UI")]
    [SerializeField] private GameObject sellConfirmPanel;
    [SerializeField] private Slider priceSlider;
    [SerializeField] private TMP_Text priceValueText;
    [SerializeField] private TMP_Text sellItemNameText;
    [SerializeField] private Button confirmSellButton;

    [Header("Inventory Panel (Investigate)")]
    public GameObject inventoryPanel;
    public Transform inventoryListParent;
    public GameObject inventoryRowPrefab;
    [Header("Objective Discovery Indicator")]
    [SerializeField] private GameObject objectiveDiscoveredStar;
    [SerializeField] private float starVisibleSeconds = 2f;
    [Header("Inventory Purchase Indicator")]
    [SerializeField] private GameObject inventoryStar;
    [SerializeField] private float inventoryStarVisibleSeconds = 2f;
    [Header("Coin Floating Text")]
    [SerializeField] private GameObject floatingCoinTextPrefab;
    [SerializeField] private Transform floatingTextSpawnPoint;
    [Header("Purchase Coin Burst VFX")]
    [SerializeField] private Sprite purchaseCoinSprite;
    [SerializeField] private int purchaseCoinBurstCount = 10;
    [SerializeField] private float purchaseCoinBurstDuration = 0.7f;
    [SerializeField] private float purchaseCoinBurstRadius = 150f;
    [SerializeField] private float purchaseCoinArcHeight = 55f;
    [SerializeField] private float purchaseCoinStartScale = 0.65f;
    [SerializeField] private float purchaseCoinEndScale = 0.18f;
    [SerializeField] private Vector2 purchaseCoinSize = new Vector2(34f, 34f);
    [SerializeField] private Color purchaseCoinColor = new Color(1f, 0.78f, 0.16f, 1f);
    [Header("Known Recipes UI")]
    [SerializeField] private GameObject knownRecipesPanel;
    [SerializeField] private Transform knownRecipesParent;
    [SerializeField] private GameObject knownRecipePrefab;
    [SerializeField] private GameObject bookCanvasRoot;
    [SerializeField] private Canvas bookCanvas;
    [SerializeField] private GameObject bookRoot;
    [SerializeField] private Button knownRecipesOpenButton;
    [SerializeField] private CanvasGroup bookCanvasGroup;
    [SerializeField] private Vector2 knownRecipeStartPosition = new Vector2(-260f, 260f);
    [SerializeField] private Vector2 knownRecipeSpacing = new Vector2(260f, 230f);
    [SerializeField] private Vector2 knownRecipeCloneSize = new Vector2(230f, 190f);
    [Tooltip("Position of the three ingredient names or ??? labels inside every recipe clone.")]
    [SerializeField] private Vector2 knownRecipeIngredientsPosition = new Vector2(52f, 8f);
    [Tooltip("Scales the complete Known Recipes title and grid as one unit.")]
    [SerializeField] private Vector3 knownRecipesWholeScale = Vector3.one;
    [Tooltip("Scales each recipe card and all of its contents.")]
    [SerializeField] private Vector3 knownRecipeItemScale = Vector3.one;
    [SerializeField] private Color knownRecipeOilColor = new Color(0.28f, 0.17f, 0.05f, 1f);
    [SerializeField] private Color knownRecipeHerbColor = new Color(0.12f, 0.72f, 0.32f, 1f);
    [SerializeField] private Color knownRecipeGemColor = new Color(0.35f, 0.16f, 0.82f, 1f);
    [SerializeField] private Color knownRecipeUnknownProductColor = new Color(0.62f, 0.62f, 0.62f, 1f);

    public static UnityAction<Sprite> OnItemBought;
    public static UnityAction OnSuccessfulMerge;
    public static UnityAction OnFailedMerge;
    public static UnityAction <bool> OnItemSold;
    public static UnityAction<string, ItemCategory> OnItemAdded;


    private InventoryItem pendingSellItem;
    private List<InventoryItem> inventory = new List<InventoryItem>();
    private List<InventoryItem> selectedCraftingItems = new List<InventoryItem>();
    private int konamiIndex = 0;
    private Dictionary<MarketItem, int> marketStock = new Dictionary<MarketItem, int>();
    private Market currentMarket;
    private bool isMergeAnimationPlaying = false;
    private bool craftingExitRequired;
    private Canvas purchaseCoinVfxCanvas;
    private CoroutineRunner purchaseCoinVfxRunner;
    private Sprite generatedPurchaseCoinSprite;


    private HashSet<string> discoveredItems = new HashSet<string>();
    private bool hasUnseenNewItem = false;
    private HashSet<string> lockedItemsToday = new HashSet<string>();

    [Header("Inventory Button")]
    [SerializeField] private Button inventoryButton;
    public ButtonBreather inventoryBreather;

    [Header("Day Night Cycle UI")]
    [SerializeField] private Vector2 dayNightCyclePosition = new Vector2(0f, -18f);
    [SerializeField] private Vector2 dayNightCycleSize = new Vector2(108f, 128f);
    [SerializeField] private float dayNightCycleRotation;
    [SerializeField] private Vector3 dayNightCycleScale = Vector3.one;
    [SerializeField] private DayNightPhase dayNightStartingPhase = DayNightPhase.Night;
    [SerializeField] private Vector2 dayNightClockFacePosition = Vector2.zero;
    [SerializeField] private Vector2 dayNightClockFaceSize = new Vector2(108f, 128f);
    [SerializeField] private Vector3 dayNightClockFaceScale = Vector3.one;
    [SerializeField] private float dayNightClockFaceRotation;
    [SerializeField] private Vector2 dayNightClockCirclePosition = Vector2.zero;
    [SerializeField] private Vector2 dayNightClockCircleSize = new Vector2(108f, 128f);
    [SerializeField] private Vector3 dayNightClockCircleScale = Vector3.one;
    [SerializeField] private float dayNightClockCircleRotation;
    [SerializeField] private Vector2 dayNightClockArrowPosition = new Vector2(-54f, 0f);
    [SerializeField] private Vector2 dayNightClockArrowSize = new Vector2(108f, 128f);
    [SerializeField] private Vector2 dayNightClockArrowPivot = new Vector2(0f, 0.5f);
    [SerializeField] private Vector3 dayNightClockArrowScale = Vector3.one;

    [Header("Family Market Right UI Block")]
    [SerializeField] private Vector2 familyMarketRightUiPosition = new Vector2(500f, 30f);
    [SerializeField] private Vector2 familyMarketRightUiSize = new Vector2(900f, 980f);
    [SerializeField] private Vector3 familyMarketRightUiScale = Vector3.one;
    [SerializeField] private float familyMarketRightUiRotation;
    [SerializeField] private Vector2 familyMarketLeftArrowPosition = new Vector2(-920f, -330f);
    [SerializeField] private Vector2 familyMarketRightArrowPosition = new Vector2(-70f, -330f);
    [SerializeField] private Vector2 familyMarketArrowSize = new Vector2(86f, 86f);
    [SerializeField] private Vector3 familyMarketArrowScale = Vector3.one;
    [SerializeField] private float familyMarketArrowRotation;
    [Header("Family Market Inventory Button")]
    [SerializeField] private Sprite familyMarketInventoryIcon;
    [SerializeField] private Vector2 familyMarketInventoryButtonPosition = new Vector2(765f, 315f);
    [SerializeField] private Vector2 familyMarketInventoryButtonSize = new Vector2(120f, 120f);
    [SerializeField] private Vector3 familyMarketInventoryButtonScale = Vector3.one;
    [SerializeField] private float familyMarketInventoryButtonRotation;

    [Header("Day Transition")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int gameOverDay = 20;
    [SerializeField] private string dayTextFormat = "Day {0}";
    [SerializeField] private Color dayTextColor = Color.white;
    [SerializeField] private float dayTextFontSize = 120f;
    [SerializeField] private Vector3 dayTextScale = Vector3.one;
    [SerializeField] private float dayTextRotation;
    [SerializeField] private float dayScreenFadeDuration = 0.35f;
    [SerializeField] private float dayScreenHoldDuration = 1.25f;

    [Header("Game Over Screen")]
    [SerializeField] private string gameOverText = "GAME OVER";
    [SerializeField] private Color gameOverTextColor = Color.red;
    [SerializeField] private float gameOverTextFontSize = 142f;
    [SerializeField] private string playAgainButtonText = "Play Again";
    [SerializeField] private Color playAgainButtonColor = new Color(0.12f, 0.02f, 0.02f, 0.92f);
    [SerializeField] private Color playAgainButtonTextColor = Color.white;

    private bool isEndingDay;
    private Canvas dayTransitionCanvas;
    private RectTransform sellPanelRightUiRect;
    private Image sellPanelRightUiImage;
    private RectTransform sellPanelInventoryButtonRect;
    private Image sellPanelInventoryButtonImage;
    private Sprite sellerRightUiSprite;

    public List<InventoryItem> GetInventoryItems()
    {
        return inventory;
    }

    public Vector2 FamilyMarketRightUiPosition => familyMarketRightUiPosition;
    public Vector2 FamilyMarketRightUiSize => familyMarketRightUiSize;
    public Vector3 FamilyMarketRightUiScale => familyMarketRightUiScale;
    public float FamilyMarketRightUiRotation => familyMarketRightUiRotation;
    public Vector2 FamilyMarketLeftArrowPosition => familyMarketLeftArrowPosition;
    public Vector2 FamilyMarketRightArrowPosition => familyMarketRightArrowPosition;
    public Vector2 FamilyMarketArrowSize => familyMarketArrowSize;
    public Vector3 FamilyMarketArrowScale => familyMarketArrowScale;
    public float FamilyMarketArrowRotation => familyMarketArrowRotation;
    public Sprite FamilyMarketInventoryIcon => familyMarketInventoryIcon;
    public Vector2 FamilyMarketInventoryButtonPosition => familyMarketInventoryButtonPosition;
    public Vector2 FamilyMarketInventoryButtonSize => familyMarketInventoryButtonSize;
    public Vector3 FamilyMarketInventoryButtonScale => familyMarketInventoryButtonScale;
    public float FamilyMarketInventoryButtonRotation => familyMarketInventoryButtonRotation;

    public ObjectiveManager objectiveManager;

    public void OpenKnownRecipes()
    {
        knownRecipesPanel.SetActive(true);
        PopulateKnownRecipesUI();
    }

    public void CloseKnownRecipes()
    {
        knownRecipesPanel.SetActive(false);
        ApplyMarketReturnGate();
    }

    private void LateUpdate()
    {
        ApplyMarketReturnGate();
    }

    private void ApplyMarketReturnGate()
    {
        if (craftingExitRequired && endDayButton != null)
            endDayButton.interactable = false;
    }

    public bool IsKnownRecipesOpen()
    {
        return knownRecipesPanel != null && knownRecipesPanel.activeInHierarchy;
    }

    public void OpenKnownRecipesBookFromMarket()
    {
        if (bookRoot != null)
            bookRoot.SetActive(true);

        if (bookCanvasGroup != null)
        {
            bookCanvasGroup.alpha = 1f;
            bookCanvasGroup.interactable = true;
            bookCanvasGroup.blocksRaycasts = true;
        }

        if (knownRecipesOpenButton != null)
            knownRecipesOpenButton.onClick.Invoke();
        else
            OpenKnownRecipes();
    }

    public void PrepareBookCanvasForFamilyMarket()
    {
        if (bookCanvasRoot != null)
            bookCanvasRoot.SetActive(true);

        if (bookCanvas != null)
        {
            bookCanvas.overrideSorting = true;
            bookCanvas.sortingOrder = 160;
        }

        if (bookCanvasGroup != null)
        {
            bookCanvasGroup.alpha = 1f;
            bookCanvasGroup.interactable = true;
            bookCanvasGroup.blocksRaycasts = true;
        }

        if (bookRoot != null)
            bookRoot.SetActive(true);

        if (knownRecipesOpenButton != null)
        {
            knownRecipesOpenButton.interactable = true;

            Graphic hitTarget = knownRecipesOpenButton.targetGraphic;
            if (hitTarget != null)
            {
                hitTarget.enabled = true;
                hitTarget.raycastTarget = true;
                Color transparentColor = hitTarget.color;
                transparentColor.a = 0f;
                hitTarget.color = transparentColor;
            }
        }
    }

    public void DisableLegacyMarketPresentation()
    {
        if (marketPanel == null)
            return;

        Image panelImage = marketPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }

        foreach (Transform child in marketPanel.transform)
            child.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyDayNightCycleUISettings();

        if (Application.isPlaying &&
            knownRecipesPanel != null &&
            knownRecipesPanel.activeInHierarchy &&
            knownRecipesParent != null &&
            knownRecipePrefab != null)
        {
            PopulateKnownRecipesUI();
        }
    }

    void PopulateKnownRecipesUI()
    {
        ClearChildren(knownRecipesParent);

        GameObject layoutObject = new GameObject("KnownRecipesLayout", typeof(RectTransform));
        layoutObject.transform.SetParent(knownRecipesParent, false);

        RectTransform layoutRect = layoutObject.GetComponent<RectTransform>();
        layoutRect.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRect.pivot = new Vector2(0.5f, 0.5f);
        layoutRect.anchoredPosition = Vector2.zero;
        layoutRect.sizeDelta = Vector2.zero;
        layoutRect.localScale = knownRecipesWholeScale;

        CreateKnownRecipeText(
            layoutRect,
            "KnownRecipesTitle",
            "Known recipes",
            new Vector2(0f, 390f),
            new Vector2(420f, 54f),
            32f,
            FontStyles.Bold,
            new Color(0.08f, 0.06f, 0.04f, 1f));

        for (int recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
        {
            Recipe recipe = recipes[recipeIndex];

            GameObject obj = Instantiate(knownRecipePrefab, layoutRect);
            RectTransform recipeRect = obj.GetComponent<RectTransform>();
            int column = recipeIndex % KnownRecipeColumns;
            int row = recipeIndex / KnownRecipeColumns;

            recipeRect.localScale = knownRecipeItemScale;
            recipeRect.anchorMin = new Vector2(0.5f, 0.5f);
            recipeRect.anchorMax = new Vector2(0.5f, 0.5f);
            recipeRect.sizeDelta = knownRecipeCloneSize;
            recipeRect.anchoredPosition = new Vector2(
                knownRecipeStartPosition.x + column * knownRecipeSpacing.x,
                knownRecipeStartPosition.y - row * knownRecipeSpacing.y);

            Image recipeBackground = obj.GetComponent<Image>();
            if (recipeBackground == null)
                recipeBackground = obj.AddComponent<Image>();
            recipeBackground.color = new Color(0.12f, 0.08f, 0.04f, 0.16f);
            recipeBackground.raycastTarget = false;

            bool recipeDiscovered = discoveredRecipes.Contains(NormalizeName(recipe.potionName));
            Transform resultIconTransform = obj.transform.Find("ResultIcon");
            if (resultIconTransform != null)
            {
                Image resultIcon = resultIconTransform.GetComponent<Image>();
                resultIcon.sprite = recipeDiscovered ? recipe.icon : null;
                resultIcon.color = recipeDiscovered ? Color.white : knownRecipeUnknownProductColor;
                resultIcon.enabled = true;
                resultIcon.preserveAspect = recipeDiscovered;
                resultIcon.raycastTarget = false;

                RectTransform iconRect = resultIconTransform.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(-64f, 35f);
                iconRect.sizeDelta = new Vector2(76f, 64f);

                if (!recipeDiscovered)
                {
                    CreateKnownRecipeText(
                        resultIconTransform,
                        "UnknownProduct",
                        "?",
                        Vector2.zero,
                        iconRect.sizeDelta,
                        46f,
                        FontStyles.Normal,
                        new Color(0.08f, 0.08f, 0.08f, 1f));
                }
            }

            CreateKnownRecipeText(
                obj.transform,
                "RecipeName",
                recipe.potionName,
                new Vector2(48f, 35f),
                new Vector2(112f, 68f),
                19f,
                FontStyles.Normal,
                new Color(0.08f, 0.06f, 0.04f, 1f));

            Transform ingredientsRow = obj.transform.Find("IngredientsRow");
            if (ingredientsRow != null)
            {
                LayoutGroup lg = ingredientsRow.GetComponent<LayoutGroup>();
                if (lg != null) Destroy(lg);

                RectTransform rowRect = ingredientsRow.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                rowRect.localScale = Vector3.one;
                rowRect.localRotation = Quaternion.identity;
                rowRect.anchoredPosition = knownRecipeIngredientsPosition;
                rowRect.sizeDelta = new Vector2(178f, 48f);
                rowRect.SetAsLastSibling();

                for (int ingredientIndex = 0; ingredientIndex < 3; ingredientIndex++)
                {
                    if (ingredientIndex >= recipe.ingredients.Count)
                        continue;

                    string ingredientName = recipe.ingredients[ingredientIndex];
                    bool ingredientDiscovered = recipeDiscovered ||
                        discoveredRecipeIngredientSlots.Contains(
                            GetRecipeIngredientSlotKey(recipe, ingredientIndex));

                    CreateKnownRecipeIngredientSlot(
                        ingredientsRow,
                        ingredientIndex,
                        ingredientName,
                        ingredientDiscovered);
                }
            }
        }
    }

    private void CreateKnownRecipeText(
        Transform parent,
        string objectName,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle,
        Color? textColor = null)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor ?? Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
    }

    private void CreateKnownRecipeIngredientSlot(
        Transform parent,
        int ingredientIndex,
        string ingredientName,
        bool discovered)
    {
        GameObject slotObject = new GameObject(
            $"Ingredient{ingredientIndex + 1}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.anchoredPosition = new Vector2(-57f + ingredientIndex * 57f, 0f);
        slotRect.sizeDelta = new Vector2(48f, 42f);

        Image slotImage = slotObject.GetComponent<Image>();
        slotImage.raycastTarget = false;
        slotImage.color = GetKnownRecipeIngredientColor(ingredientName);
        slotImage.preserveAspect = discovered;

        if (discovered)
        {
            slotImage.sprite = GetIconByNameInsensitive(ingredientName);
            if (slotImage.sprite != null)
                slotImage.color = Color.white;
            else
                slotImage.color = GetKnownRecipeIngredientColor(ingredientName);
        }
        else
        {
            CreateKnownRecipeText(
                slotObject.transform,
                "UnknownIngredient",
                "?",
                Vector2.zero,
                slotRect.sizeDelta,
                25f,
                FontStyles.Bold,
                Color.white);
        }
    }

    private Color GetKnownRecipeIngredientColor(string ingredientName)
    {
        string normalizedIngredient = NormalizeName(ingredientName);

        foreach (Market market in markets)
        {
            foreach (MarketItem item in market.items)
            {
                if (NormalizeName(item.itemName) != normalizedIngredient)
                    continue;

                switch (item.category)
                {
                    case ItemCategory.Oils:
                        return knownRecipeOilColor;
                    case ItemCategory.Herbs:
                        return knownRecipeHerbColor;
                    case ItemCategory.Gems:
                        return knownRecipeGemColor;
                }
            }
        }

        return Color.gray;
    }

    public bool DiscoverRecipeIngredient(string ingredientName)
    {
        string normalizedIngredient = NormalizeName(ingredientName);
        List<int> matchingRecipeIndexes = new List<int>();

        for (int recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
        {
            Recipe recipe = recipes[recipeIndex];
            if (discoveredRecipes.Contains(NormalizeName(recipe.potionName)))
                continue;

            for (int ingredientIndex = 0; ingredientIndex < recipe.ingredients.Count; ingredientIndex++)
            {
                bool isMatchingIngredient =
                    NormalizeName(recipe.ingredients[ingredientIndex]) == normalizedIngredient;
                bool isAlreadyDiscovered = discoveredRecipeIngredientSlots.Contains(
                    GetRecipeIngredientSlotKey(recipe, ingredientIndex));

                if (isMatchingIngredient && !isAlreadyDiscovered)
                {
                    matchingRecipeIndexes.Add(recipeIndex);
                    break;
                }
            }
        }

        if (matchingRecipeIndexes.Count == 0)
            return false;

        int selectedRecipeIndex = matchingRecipeIndexes[Random.Range(0, matchingRecipeIndexes.Count)];
        Recipe selectedRecipe = recipes[selectedRecipeIndex];

        for (int ingredientIndex = 0; ingredientIndex < selectedRecipe.ingredients.Count; ingredientIndex++)
        {
            if (NormalizeName(selectedRecipe.ingredients[ingredientIndex]) != normalizedIngredient)
                continue;

            discoveredRecipeIngredientSlots.Add(
                GetRecipeIngredientSlotKey(selectedRecipe, ingredientIndex));
            break;
        }

        if (knownRecipesPanel.activeInHierarchy)
            PopulateKnownRecipesUI();

        return true;
    }

    private void DiscoverRecipe(Recipe recipe)
    {
        discoveredRecipes.Add(NormalizeName(recipe.potionName));

        if (knownRecipesPanel.activeInHierarchy)
            PopulateKnownRecipesUI();
    }

    private static string GetRecipeIngredientSlotKey(Recipe recipe, int ingredientIndex)
    {
        return $"{NormalizeName(recipe.potionName)}|{ingredientIndex}";
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Finds the icon for an item name, ignoring case and spaces
    /// </summary>
    Sprite GetIconByNameInsensitive(string itemName)
    {
        string cleanedName = itemName.Replace(" ", "").ToLower();

        // Check market items
        foreach (var market in markets)
        {
            foreach (var item in market.items)
            {
                if (item.itemName.Replace(" ", "").ToLower() == cleanedName)
                    return item.icon;
            }
        }

        // Check recipes (crafted items)
        foreach (var recipe in recipes)
        {
            if (recipe.potionName.Replace(" ", "").ToLower() == cleanedName)
                return recipe.icon;
        }

        return null;
    }
    // ------------------- START -------------------
    void Start()
    {
        ApplyDayNightCycleUISettings();
        DayNightCycleUI.SetPhase(dayNightStartingPhase, true);
        FamilyMarketUI.Attach(this);
        EnsureFrontCanvas(itemsPanel, ShopItemsSortingOrder);
        EnsureSellPanelRightUI();
        RandomizeMarketStock();
        inventoryBreather = inventoryButton.GetComponent<ButtonBreather>();
        PopulateInventoryPanel();
        UpdateCoinsUI();
        priceSlider.minValue = 0;
        priceSlider.maxValue = 100;
        priceSlider.wholeNumbers = true;
        
        priceSlider.onValueChanged.RemoveAllListeners();
        priceSlider.onValueChanged.AddListener(OnPriceSliderChanged);

    }

    private void ApplyDayNightCycleUISettings()
    {
        DayNightCycleUI.SetLayout(dayNightCyclePosition, dayNightCycleSize, dayNightCycleRotation, dayNightCycleScale);
        DayNightCycleUI.SetPartLayout(
            dayNightClockFacePosition,
            dayNightClockFaceSize,
            dayNightClockFaceScale,
            dayNightClockFaceRotation,
            dayNightClockCirclePosition,
            dayNightClockCircleSize,
            dayNightClockCircleScale,
            dayNightClockCircleRotation,
            dayNightClockArrowPosition,
            dayNightClockArrowSize,
            dayNightClockArrowPivot,
            dayNightClockArrowScale);
    }

    // ------------------- MARKET -------------------
    public void StartMarketPhase()
    {
        DayNightCycleUI.SetPhase(DayNightPhase.Day);
        FamilyMarketUI.Attach(this);
        DisableLegacyMarketPresentation();
        PrepareBookCanvasForFamilyMarket();

        marketPanel.SetActive(true);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);
    }

    public void OpenMarket(Market market)
    {
        DayNightCycleUI.SetPhase(DayNightPhase.Day);

        currentMarket = market;
        //marketPanel.SetActive(false);
        itemsPanel.SetActive(true);
        EnsureFrontCanvas(itemsPanel, ShopItemsSortingOrder);
        ad.PlaySfx(vol, SFX.EnteredShop, pitch);

        ClearChildren(itemsButtonsParent);

        foreach (MarketItem item in market.items)
        {
            GameObject btn = Instantiate(buttonPrefab, itemsButtonsParent);

            var tooltip = btn.GetComponent<ItemHoverTooltip>();
            tooltip.marketItem = item;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            int stock = marketStock[item];
            txt.text = "x " + stock;
            ApplyCategoryStyle(btn, item.category);

            Button button = btn.GetComponent<Button>();
            button.interactable = stock > 0;

            Image iconImage = btn.transform.Find("Icon").GetComponent<Image>();
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;

            button.onClick.AddListener(() =>
            {
                BuyItem(item);
            });
        }
    }

    private void EnsureFrontCanvas(GameObject panel, int sortingOrder)
    {
        if (panel == null) return;

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = panel.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (panel.GetComponent<GraphicRaycaster>() == null)
        {
            panel.AddComponent<GraphicRaycaster>();
        }
    }
    void RandomizeMarketStock()
    {
        marketStock.Clear();

        foreach (Market market in markets)
        {
            foreach (MarketItem item in market.items)
            {
                int amount = Random.Range(item.minAmount, item.maxAmount + 1);
                marketStock[item] = amount;
            }
        }
    }
    void BuyItem(MarketItem item)
    {
        if (coins < item.price) return;
        if (marketStock[item] <= 0) return;

        coins -= item.price;
        marketStock[item]--;

        AddToInventory(item.itemName, item.category, item.description, item.icon);

        ShowInventoryStar();
        PopulateInventoryPanel();
        UpdateCoinsUI();
        ShowFloatingCoins(-item.price);
        PlayPurchaseCoinBurst();
        ad.PlaySfx(vol, SFX.Buying, pitch);

        OnItemBought?.Invoke(item.icon);
        objectiveManager.UpdateTasksFromInventory(GetInventoryItems());

        RefreshMarketItemsUI(); // <-- just refresh buttons and counts
        FamilyMarketUI.RefreshIfVisible();
    }

    public Market GetMarketForCategory(ItemCategory category)
    {
        foreach (Market market in markets)
        {
            if (market.items.Count > 0 && market.items[0].category == category)
                return market;
        }

        return null;
    }

    public int GetMarketStock(MarketItem item)
    {
        return item != null && marketStock.TryGetValue(item, out int stock) ? stock : 0;
    }

    public void BuyMarketItemFromFamilyUI(MarketItem item)
    {
        BuyItem(item);
    }

    public void InvokeMarketShopButton()
    {
        if (marketShopButton != null && marketShopButton.interactable)
            marketShopButton.onClick.Invoke();
    }

    public void InvokeInventoryButton()
    {
        if (inventoryButton != null && inventoryButton.interactable)
            inventoryButton.onClick.Invoke();
    }

    private void EnsureSellPanelRightUI()
    {
        if (sellPanel == null)
            return;

        if (sellPanelRightUiRect == null)
        {
            Transform existingBlock = sellPanel.transform.Find("Sell Panel Right UI Block");
            if (existingBlock != null)
            {
                sellPanelRightUiRect = existingBlock as RectTransform;
                sellPanelRightUiImage = existingBlock.GetComponent<Image>();
            }
        }

        if (sellPanelRightUiRect == null)
        {
            Debug.LogWarning("Sell Panel Right UI Block is missing from the SellPanel scene hierarchy.");
        }
        else
        {
            if (sellPanelRightUiImage == null)
                sellPanelRightUiImage = sellPanelRightUiRect.GetComponent<Image>();

            if (sellPanelRightUiImage != null)
            {
                sellPanelRightUiImage.sprite = LoadSellerRightUiSprite();
                sellPanelRightUiImage.color = Color.white;
                sellPanelRightUiImage.preserveAspect = true;
                sellPanelRightUiImage.raycastTarget = false;
            }
        }

        if (sellPanelInventoryButtonRect == null)
        {
            Transform existingButton = sellPanel.transform.Find("Sell Panel Inventory Button");
            if (existingButton != null)
            {
                sellPanelInventoryButtonRect = existingButton as RectTransform;
                sellPanelInventoryButtonImage = existingButton.GetComponent<Image>();
            }
        }

        if (sellPanelInventoryButtonRect == null)
        {
            Debug.LogWarning("Sell Panel Inventory Button is missing from the SellPanel scene hierarchy.");
        }
        else
        {
            if (sellPanelInventoryButtonImage == null)
                sellPanelInventoryButtonImage = sellPanelInventoryButtonRect.GetComponent<Image>();

            if (sellPanelInventoryButtonImage != null)
            {
                sellPanelInventoryButtonImage.sprite = familyMarketInventoryIcon;
                sellPanelInventoryButtonImage.color = Color.white;
                sellPanelInventoryButtonImage.preserveAspect = true;
                sellPanelInventoryButtonImage.raycastTarget = true;
            }

            Button button = sellPanelInventoryButtonRect.GetComponent<Button>();
            if (button != null && sellPanelInventoryButtonImage != null)
                button.targetGraphic = sellPanelInventoryButtonImage;
        }

    }


    private Sprite LoadSellerRightUiSprite()
    {
        if (sellerRightUiSprite != null)
            return sellerRightUiSprite;

        const string resourcePath = "FamilyMarket/SellerRightUI";
        sellerRightUiSprite = Resources.Load<Sprite>(resourcePath);
        if (sellerRightUiSprite != null)
            return sellerRightUiSprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
        {
            sellerRightUiSprite = sprites[0];
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i].name == "SellerRightUI" || sprites[i].name == "SellerRightUI_0")
                {
                    sellerRightUiSprite = sprites[i];
                    break;
                }
            }
        }

        return sellerRightUiSprite;
    }

    void RefreshMarketItemsUI()
    {
        if (currentMarket == null ||
            itemsButtonsParent == null ||
            itemsPanel == null ||
            !itemsPanel.activeInHierarchy)
        {
            return;
        }

        ClearChildren(itemsButtonsParent);

        foreach (MarketItem item in currentMarket.items)
        {
            GameObject btn = Instantiate(buttonPrefab, itemsButtonsParent);

            var tooltip = btn.GetComponent<ItemHoverTooltip>();
            tooltip.marketItem = item;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            int stock = marketStock[item];
            txt.text = "x " + stock;
            ApplyCategoryStyle(btn, item.category);

            Button button = btn.GetComponent<Button>();
            button.interactable = stock > 0;

            Image iconImage = btn.transform.Find("Icon").GetComponent<Image>();
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;

            button.onClick.AddListener(() => BuyItem(item));
        }
    }

    // ------------------- CRAFTING -------------------
    public void OpenCrafting()
    {
        craftingExitRequired = true;

        if (endDayButton != null)
            endDayButton.interactable = false;

        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        sellPanel.SetActive(true);

        craftingPanel.SetActive(true);

        selectedCraftingItems.Clear();

        RefreshCraftingUI();
        RefreshSelectedItemsUI();
    }

    void RefreshCraftingUI()
    {
        ClearChildren(craftingItemsParent);

        foreach (InventoryItem item in inventory)
        {
            if (item.count <= 0) continue;
            if (IsRecipeItem(item.itemName)) continue;

            GameObject btn = Instantiate(buttonPrefab, craftingItemsParent);

            // Assign tooltip
            var tooltip = btn.GetComponent<ItemHoverTooltip>();
            tooltip.inventoryItem = item;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.gameObject.SetActive(false);

            ApplyCategoryStyle(btn, item.category);

            Image iconImage = btn.transform.Find("Icon").GetComponent<Image>();
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;

            btn.GetComponent<Button>().onClick.AddListener(() => SelectCraftingItem(item));
        }
    }

    bool IsRecipeItem(string itemName)
    {
        string cleanedItemName = itemName.Trim().ToLower();

        foreach (Recipe recipe in recipes)
        {
            if (recipe.potionName.Trim().ToLower() == cleanedItemName)
            {
                return true;
            }
        }

        return false;
    }

    void SelectCraftingItem(InventoryItem item)
    {
        TooltipManager.Instance.Hide();
        if (selectedCraftingItems.Count >= 3) return;
        if (item.count <= 0) return;

        // Add ONE UNIT of this item to crafting
        selectedCraftingItems.Add(new InventoryItem
        {
            itemName = item.itemName,
            category = item.category,
            count = 1,
            description = item.description,
            icon = item.icon
        });

        // Remove one from inventory stack
        RemoveFromInventory(item.itemName);

        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        PopulateInventoryPanel();
    }

    void RefreshSelectedItemsUI()
    {
        Canvas selectedItemsCanvas = selectedItemsParent.GetComponent<Canvas>();
        if (selectedItemsCanvas != null &&
            selectedItemsParent.GetComponent<GraphicRaycaster>() == null)
        {
            selectedItemsParent.gameObject.AddComponent<GraphicRaycaster>();
        }

        ClearChildren(selectedItemsParent);

        for (int i = 0; i < selectedCraftingItems.Count; i++)
        {
            GameObject btnObj = Instantiate(selectedItemTextPrefab, selectedItemsParent);
            InventoryItem selectedItem = selectedCraftingItems[i];
            int selectedIndex = i;

            // Get the Icon inside Button child
            Image iconImage = btnObj.transform.Find("Button/Icon")?.GetComponent<Image>();
            if (iconImage != null && selectedItem.icon != null)
            {
                iconImage.sprite = selectedItem.icon;
                iconImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"Icon missing for item {selectedItem.itemName}");
            }

            Button selectedButton = btnObj.GetComponent<Button>();
            if (selectedButton == null)
                selectedButton = btnObj.AddComponent<Button>();

            selectedButton.onClick.RemoveAllListeners();
            selectedButton.onClick.AddListener(() => UnselectCraftingItem(selectedIndex));

            ItemHoverTooltip tooltip = btnObj.GetComponent<ItemHoverTooltip>();
            if (tooltip == null)
                tooltip = btnObj.AddComponent<ItemHoverTooltip>();

            tooltip.inventoryItem = selectedItem;

            // Position button
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (selectedCraftingItems.Count == 1)
                rt.anchoredPosition = Vector2.zero;
            else if (selectedCraftingItems.Count == 2)
                rt.anchoredPosition = (i == 0) ? triangleLeftTop : triangleRightTop;
            else
            {
                if (i == 0) rt.anchoredPosition = triangleLeftTop;
                if (i == 1) rt.anchoredPosition = triangleRightTop;
                if (i == 2) rt.anchoredPosition = triangleCenterBottom;
            }
        }
    }

    private void UnselectCraftingItem(int selectedIndex)
    {
        if (isMergeAnimationPlaying) return;
        if (selectedIndex < 0 || selectedIndex >= selectedCraftingItems.Count) return;

        TooltipManager.Instance.Hide();
        ContextBlocker.IgnoreCloseForCurrentFrame();

        InventoryItem selectedItem = selectedCraftingItems[selectedIndex];
        InventoryItem existing = inventory.Find(i => i.itemName == selectedItem.itemName);

        if (existing != null)
        {
            existing.count += 1;
        }
        else
        {
            inventory.Add(new InventoryItem
            {
                itemName = selectedItem.itemName,
                count = 1,
                category = selectedItem.category,
                description = selectedItem.description,
                icon = selectedItem.icon
            });
        }

        selectedCraftingItems.RemoveAt(selectedIndex);
        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        PopulateInventoryPanel();
    }

    public void MergeItems()
    {
        if (selectedCraftingItems.Count < 2) return;
        if (isMergeAnimationPlaying) return;

        StartCoroutine(MergeAnimationCoroutine());
    }

    private IEnumerator MergeAnimationCoroutine()
    {
        isMergeAnimationPlaying = true;

        List<Image> imagesToAnimate = new List<Image>();
        List<Material> dissolveMaterials = new List<Material>();
        List<RectTransform> selectedRects = new List<RectTransform>();
        List<Vector3> startScales = new List<Vector3>();
        List<Vector2> startPositions = new List<Vector2>();
        List<CanvasGroup> selectedGroups = new List<CanvasGroup>();

        foreach (Transform selectedItemTransform in selectedItemsParent)
        {
            RectTransform selectedRect = selectedItemTransform.GetComponent<RectTransform>();
            if (selectedRect != null)
            {
                selectedRects.Add(selectedRect);
                startScales.Add(selectedRect.localScale);
                startPositions.Add(selectedRect.anchoredPosition);
            }

            CanvasGroup canvasGroup = selectedItemTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = selectedItemTransform.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            selectedGroups.Add(canvasGroup);

            Image[] images = selectedItemTransform.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image == null || !image.enabled)
                {
                    continue;
                }

                Material dissolveMaterial = new Material(image.material);
                dissolveMaterial.SetFloat("_LifeTime", MergeDissolveStart);

                if (dissolveMaterial.HasProperty("_EdgeWidth"))
                {
                    dissolveMaterial.SetFloat("_EdgeWidth", 0.65f);
                }

                if (dissolveMaterial.HasProperty("_DissolvePower"))
                {
                    dissolveMaterial.SetFloat("_DissolvePower", 70f);
                }

                if (dissolveMaterial.HasProperty("_DissolveColor"))
                {
                    dissolveMaterial.SetColor("_DissolveColor", new Color(0.2f, 1f, 0.42f, 1f));
                }

                image.material = dissolveMaterial;
                imagesToAnimate.Add(image);
                dissolveMaterials.Add(dissolveMaterial);
            }
        }

        float duration = 0.72f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float value = Mathf.Lerp(MergeDissolveStart, MergeDissolveEnd, easedT);

            for (int i = 0; i < dissolveMaterials.Count; i++)
            {
                dissolveMaterials[i].SetFloat("_LifeTime", value);
                imagesToAnimate[i].SetMaterialDirty();
            }

            float pulse = Mathf.Sin(t * Mathf.PI);
            float scale = Mathf.Lerp(1.06f, 0.68f, easedT) + pulse * 0.04f;
            AnimateSelectedRects(selectedRects, startScales, startPositions, scale, easedT);
            SetSelectedGroupAlpha(selectedGroups, 1f - Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        for (int i = 0; i < dissolveMaterials.Count; i++)
        {
            dissolveMaterials[i].SetFloat("_LifeTime", MergeDissolveEnd);
            imagesToAnimate[i].SetMaterialDirty();
        }

        SetSelectedGroupAlpha(selectedGroups, 0f);
        for (int i = 0; i < selectedRects.Count; i++)
        {
            selectedRects[i].gameObject.SetActive(false);
        }

        CraftSelectedItems();
        isMergeAnimationPlaying = false;
    }

    private void AnimateSelectedRects(List<RectTransform> rects, List<Vector3> startScales, List<Vector2> startPositions, float scale, float moveToCenterAmount)
    {
        for (int i = 0; i < rects.Count; i++)
        {
            rects[i].localScale = startScales[i] * scale;
            rects[i].anchoredPosition = Vector2.Lerp(startPositions[i], Vector2.zero, moveToCenterAmount);
        }
    }

    private void SetSelectedGroupAlpha(List<CanvasGroup> groups, float alpha)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].alpha = alpha;
        }
    }

    private void CraftSelectedItems()
    {
        bool craftedSomething = false;

        foreach (Recipe recipe in recipes)
        {
            if (recipe.ingredients.Count != selectedCraftingItems.Count) continue;

            Dictionary<string, int> needed = new Dictionary<string, int>();
            foreach (string ing in recipe.ingredients)
            {
                string key = ing.Trim().ToLower();
                if (!needed.ContainsKey(key)) needed[key] = 0;
                needed[key]++;
            }

            Dictionary<string, int> provided = new Dictionary<string, int>();
            foreach (InventoryItem item in selectedCraftingItems)
            {
                string key = item.itemName.Trim().ToLower();
                if (!provided.ContainsKey(key)) provided[key] = 0;
                provided[key]++;
            }

            bool match = true;
            foreach (var pair in needed)
            {
                if (!provided.TryGetValue(pair.Key, out int count) || count != pair.Value)
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                ad.PlaySfx(vol, SFX.MergePotion, pitch);
                AddToInventory(recipe.potionName, recipe.category, "", recipe.icon);
                DiscoverRecipe(recipe);
                craftedSomething = true;
                OnSuccessfulMerge?.Invoke();
                break;
            }
        }

        if (!craftedSomething)
        {
            AddToInventory(junkItemName, ItemCategory.Junk, "", junkIcon);
            ad.PlaySfx(vol, SFX.JunkMerge, pitch);
            OnFailedMerge?.Invoke();
        }

        selectedCraftingItems.Clear();

        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        PopulateInventoryPanel();
        FindObjectOfType<ObjectiveManager>().CompleteMission(MissionType.MergeItems);
    }

    public void CancelCrafting()
    {
        ReturnCraftingItemsToInventory();

        selectedCraftingItems.Clear();

        craftingExitRequired = false;

        if (endDayButton != null)
            endDayButton.interactable = true;

        DayNightCycleUI.SetPhase(DayNightPhase.Night);

        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        PopulateInventoryPanel();
    }

    void ReturnCraftingItemsToInventory()
    {
        foreach (var item in selectedCraftingItems)
        {
            // Add one unit back to inventory, preserving icon and description
            InventoryItem existing = inventory.Find(i => i.itemName == item.itemName);
            if (existing != null)
            {
                existing.count += 1; // increment count
            }
            else
            {
                inventory.Add(new InventoryItem
                {
                    itemName = item.itemName,
                    count = 1,
                    category = item.category,
                    description = item.description, // keep original description
                    icon = item.icon              // keep original icon
                });
            }
        }

        // Clear the selected list
        selectedCraftingItems.Clear();

        // Refresh the UI safely
        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        PopulateInventoryPanel();
    }

    // ------------------- SELL -------------------
    public void OpenSell()
    {
        if (endDayButton != null)
            endDayButton.interactable = false;

        DayNightCycleUI.SetPhase(DayNightPhase.Evening);

        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(true);
        EnsureSellPanelRightUI();
        ReturnCraftingItemsToInventory();
        RefreshSellUI();
    }
    void SellItem(InventoryItem item, int price)
    {
        coins += price;
        RemoveFromInventory(item.itemName);

        RefreshSellUI();
        PopulateInventoryPanel();
        ShowFloatingCoins(price);
        UpdateCoinsUI();
        ad.PlaySfx(vol,SFX.Selling,pitch);
        
        if (price > 10)
        {
            OnItemSold?.Invoke(true);
        }
        else
        {
            OnItemSold?.Invoke(false);
        }

            FindObjectOfType<ObjectiveManager>().CompleteMission(MissionType.SellItems);
        if (inventoryPanel.activeSelf)
            PopulateInventoryPanel();
    }

    public void RefreshSellUI()
    {
        ClearChildren(sellItemsParent);

        foreach (InventoryItem item in inventory)
        {
            GameObject btn = Instantiate(buttonPrefab, sellItemsParent);

            var tooltip = btn.GetComponent<ItemHoverTooltip>();
            tooltip.inventoryItem = item;

            // HIDE old text
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.gameObject.SetActive(false);

            ApplyCategoryStyle(btn, item.category);

            // ICON
            Transform iconTransform = btn.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                iconImage.sprite = item.icon;
                iconImage.enabled = item.icon != null;
                iconImage.raycastTarget = false; 
            }

            // COUNT TEXT
            Transform countTransform = btn.transform.Find("CountText");
            if (countTransform != null)
            {
                TMP_Text countText = countTransform.GetComponent<TMP_Text>();

                if (item.count > 1)
                    countText.text = "x " + item.count;
                else
                    countText.text = "";
            }

            Button button = btn.GetComponent<Button>();

            if (lockedItemsToday.Contains(item.itemName))
            {
                button.interactable = false;
            }
            else
            {
                button.interactable = true;
                button.onClick.AddListener(() => OnSellClicked(item));
            }
        }
    }
    void OnPriceSliderChanged(float value)
    {
        int price = Mathf.RoundToInt(value);
        priceValueText.text = price.ToString();
    }

    void OnSellClicked(InventoryItem item)
    {
        TooltipManager.Instance.Hide();
        if (item.itemName == junkItemName)
        {
            SellItem(item, junkSellPrice);
            return;
        }

        pendingSellItem = item;
        sellItemNameText.text = item.itemName;

        // Reset slider to something sensible
        priceSlider.value = 0;
        priceValueText.text = "0";

        sellConfirmPanel.SetActive(true);
    }



    // ------------------- INVENTORY -------------------
    void AddToInventory(string name, ItemCategory category, string description = "", Sprite icon = null)
    {
        bool isNewItem = !discoveredItems.Contains(name);

        ShowObjectiveDiscoveryStar();

        InventoryItem existing = inventory.Find(i => i.itemName == name);

        if (existing != null)
        {
            existing.count++;
        }
        else
        {
            inventory.Add(new InventoryItem
            {
                itemName = name,
                count = 1,
                category = category,
                description = description,
                icon = icon
            });
        }

        if (isNewItem)
        {
            discoveredItems.Add(name);

            hasUnseenNewItem = true;

            if (inventoryBreather != null)
                inventoryBreather.StartBreathing();
        }

        if (inventoryButton != null)
            inventoryButton.interactable = true;

        OnItemAdded?.Invoke(name, category);
    }

    void RemoveFromInventory(string name)
    {
        InventoryItem existing = inventory.Find(i => i.itemName == name);
        if (existing == null) return;

        existing.count--;
        if (existing.count <= 0) inventory.Remove(existing);
    }
    public void UpdateCoinsUI()
    {
        coinsText.text = $"{coins}";
    }
    public void OpenInventoryPanel()
    {
        inventoryPanel.SetActive(true);
        PopulateInventoryPanel();

        if (hasUnseenNewItem)
        {
            hasUnseenNewItem = false;

            if (inventoryBreather != null)
                inventoryBreather.StopBreathing();
        }
    }

    public void PopulateInventoryPanel()
    {
        foreach (Transform child in inventoryListParent)
            Destroy(child.gameObject);

        foreach (InventoryItem item in inventory)
        {
            GameObject row = Instantiate(inventoryRowPrefab, inventoryListParent);

            // Assign texts properly
            Image iconImage = row.transform.Find("Icon").GetComponent<Image>();
            TMP_Text itemCountText = row.transform.Find("ItemCountText").GetComponent<TMP_Text>();
            Button investigateBtn = row.transform.Find("InvestigateButton").GetComponent<Button>();

            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
            itemCountText.text = "x" + item.count;

            // Setup investigate button
            FillInvestigateButton fillScript = investigateBtn.GetComponent<FillInvestigateButton>();
            fillScript.itemName = item.itemName;

            // Disable if daily limit reached
            investigateBtn.interactable = objectiveManager.CanInvestigateToday() && objectiveManager.CanAffordInvestigation();
        }
    }


    // ------------------- END DAY -------------------
    public void EndDay()
    {
        if (isEndingDay)
            return;

        StartCoroutine(EndDayRoutine());
    }

    private IEnumerator EndDayRoutine()
    {
        isEndingDay = true;

        RandomizeMarketStock();
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        lockedItemsToday.Clear();
        objectiveManager.ResetDailyInvestigations();
        PopulateInventoryPanel();

        currentDay++;
        yield return ShowDayTransitionIntroRoutine(currentDay);

        bool reachedGameOverDay = currentDay >= gameOverDay;
        if (!reachedGameOverDay)
            StartMarketPhase();

        yield return FadeDayTransitionOutRoutine();

        if (reachedGameOverDay)
        {
            ShowGameOverScreen();
            isEndingDay = false;
            yield break;
        }

        DestroyDayTransitionCanvas();
        isEndingDay = false;
    }

    private IEnumerator ShowDayTransitionIntroRoutine(int day)
    {
        Canvas canvas = EnsureDayTransitionCanvas();
        Transform root = canvas.transform;
        ClearChildren(root);

        Image background = CreateOverlayImage("Day Transition Background", root, Color.black);
        background.raycastTarget = true;

        TMP_Text dayText = CreateOverlayText("Day Text", root, string.Format(dayTextFormat, day));
        dayText.color = dayTextColor;
        dayText.fontSize = dayTextFontSize;
        dayText.fontStyle = FontStyles.Bold;

        RectTransform dayRect = dayText.rectTransform;
        dayRect.anchorMin = Vector2.zero;
        dayRect.anchorMax = Vector2.one;
        dayRect.offsetMin = Vector2.zero;
        dayRect.offsetMax = Vector2.zero;
        dayRect.localScale = dayTextScale;
        dayRect.localRotation = Quaternion.Euler(0f, 0f, dayTextRotation);

        CanvasGroup group = canvas.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

        yield return FadeCanvasGroup(group, 0f, 1f, dayScreenFadeDuration);
        yield return new WaitForSecondsRealtime(dayScreenHoldDuration);
    }

    private IEnumerator FadeDayTransitionOutRoutine()
    {
        if (dayTransitionCanvas == null)
            yield break;

        CanvasGroup group = dayTransitionCanvas.GetComponent<CanvasGroup>();
        yield return FadeCanvasGroup(group, 1f, 0f, dayScreenFadeDuration);
    }

    private void ShowGameOverScreen()
    {
        Canvas canvas = EnsureDayTransitionCanvas();
        Transform root = canvas.transform;
        ClearChildren(root);

        Image background = CreateOverlayImage("Game Over Background", root, Color.black);
        background.raycastTarget = true;

        TMP_Text title = CreateOverlayText("Game Over Text", root, gameOverText);
        title.color = gameOverTextColor;
        title.fontSize = gameOverTextFontSize;
        title.fontStyle = FontStyles.Bold;

        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 110f);
        titleRect.sizeDelta = new Vector2(1200f, 220f);

        Button playAgainButton = CreatePlayAgainButton(root);
        playAgainButton.onClick.AddListener(RestartGame);

        CanvasGroup group = canvas.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        Time.timeScale = 0f;
    }

    private Canvas EnsureDayTransitionCanvas()
    {
        if (dayTransitionCanvas != null)
            return dayTransitionCanvas;

        GameObject canvasObject = new GameObject(
            "Day Transition Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        dayTransitionCanvas = canvasObject.GetComponent<Canvas>();
        dayTransitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dayTransitionCanvas.overrideSorting = true;
        dayTransitionCanvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return dayTransitionCanvas;
    }

    private Image CreateOverlayImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text CreateOverlayText(string objectName, Transform parent, string text)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24f;
        label.fontSizeMax = Mathf.Max(label.fontSize, 180f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private Button CreatePlayAgainButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Play Again", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -130f);
        rect.sizeDelta = new Vector2(330f, 92f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = playAgainButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CreateOverlayText("Play Again Text", buttonObject.transform, playAgainButtonText);
        label.color = playAgainButtonTextColor;
        label.fontSize = 38f;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, progress);
            yield return null;
        }

        group.alpha = to;
    }

    private void DestroyDayTransitionCanvas()
    {
        if (dayTransitionCanvas == null)
            return;

        Destroy(dayTransitionCanvas.gameObject);
        dayTransitionCanvas = null;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        currentDay = 1;
        DestroyDayTransitionCanvas();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------- CATEGORY STYLING -------------------
    bool TryGetCategoryStyle(ItemCategory category, out CategoryStyle style)
    {
        foreach (var s in categoryStyles)
        {
            if (s.category == category)
            {
                style = s;
                return true;
            }
        }

        style = default;
        return false;
    }

    void ApplyCategoryStyle(GameObject button, ItemCategory category)
    {
        if (!TryGetCategoryStyle(category, out var style)) return;

        Image bg = button.GetComponent<Image>();
        TMP_Text txt = button.GetComponentInChildren<TMP_Text>();

        if (bg != null) bg.color = style.backgroundColor;
        if (txt != null) txt.color = style.textColor;
    }

    void ClearChildren(Transform t)
    {
        foreach (Transform c in t)
            Destroy(c.gameObject);
    }

    public void OpenMarketByIndex(int marketIndex)
    {
        if (marketIndex < 0 || marketIndex >= markets.Count)
        {
            Debug.LogError($"Invalid market index: {marketIndex}");
            return;
        }

        OpenMarket(markets[marketIndex]);
    }

    public void ConfirmSale()
    {
        if (pendingSellItem == null) return;

        int price = Mathf.RoundToInt(priceSlider.value);

        if (!TryGetSellPriceLimits(pendingSellItem.itemName, out int min, out int max))
            return;

        if (price < min || price > max)
        {
            Debug.Log($"Blocked sale: {price} not in range {min}-{max}");

            lockedItemsToday.Add(pendingSellItem.itemName);

            pendingSellItem = null;
            sellConfirmPanel.SetActive(false);

            RefreshSellUI(); // update buttons immediately
            return;
        }

        SellItem(pendingSellItem, price);
        pendingSellItem = null;
        sellConfirmPanel.SetActive(false);
    }



    bool TryGetSellPriceLimits(string itemName, out int min, out int max)
    {
        // Check crafted potions (Recipes)
        foreach (var recipe in recipes)
        {
            if (recipe.potionName == itemName)
            {
                min = recipe.minSellPrice;
                max = recipe.maxSellPrice;
                return true;
            }
        }

        // Check raw ingredients (Market items)
        foreach (var market in markets)
        {
            foreach (var item in market.items)
            {
                if (item.itemName == itemName)
                {
                    min = item.minSellPrice;
                    max = item.maxSellPrice;
                    return true;
                }
            }
        }

        // If no limits found, block the sale
        min = 0;
        max = 0;
        return false;
    }



    public void ShowObjectiveDiscoveryStar()
    {
        if (objectiveDiscoveredStar == null) return;

        StopAllCoroutines();
        StartCoroutine(ShowStarRoutine());
    }

    private System.Collections.IEnumerator ShowStarRoutine()
    {
        objectiveDiscoveredStar.SetActive(true);
        yield return new WaitForSeconds(starVisibleSeconds);
        objectiveDiscoveredStar.SetActive(false);
    }

    public void ShowInventoryStar()
    {
        if (inventoryStar == null) return;

        StopCoroutine(nameof(ShowInventoryStarRoutine));
        StartCoroutine(ShowInventoryStarRoutine());
    }

    private System.Collections.IEnumerator ShowInventoryStarRoutine()
    {
        inventoryStar.SetActive(true);
        yield return new WaitForSeconds(inventoryStarVisibleSeconds);
        inventoryStar.SetActive(false);
    }

    public void ShowFloatingCoins(int amount)
    {
        if (floatingCoinTextPrefab == null || floatingTextSpawnPoint == null)
            return;

        GameObject obj = Instantiate(floatingCoinTextPrefab, floatingTextSpawnPoint.position, Quaternion.identity, floatingTextSpawnPoint);

        FloatingCoinText floatText = obj.GetComponent<FloatingCoinText>();

        if (amount < 0)
            floatText.SetText(amount.ToString(), Color.red);
        else
            floatText.SetText("+" + amount.ToString(), Color.yellow);
    }

    private void PlayPurchaseCoinBurst()
    {
        if (coinsText == null || purchaseCoinBurstCount <= 0 || purchaseCoinBurstDuration <= 0f)
            return;

        EnsurePurchaseCoinVfxCanvas();
        if (purchaseCoinVfxCanvas == null || purchaseCoinVfxRunner == null)
            return;

        Vector2 origin = GetCoinsTextPositionOnPurchaseCanvas();

        for (int i = 0; i < purchaseCoinBurstCount; i++)
        {
            float angle = Random.Range(15f, 345f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = Random.Range(purchaseCoinBurstRadius * 0.45f, purchaseCoinBurstRadius);
            Vector2 endPosition = origin + direction * distance;
            float delay = Random.Range(0f, 0.08f);

            purchaseCoinVfxRunner.StartCoroutine(AnimatePurchaseCoin(origin, endPosition, delay));
        }
    }

    private void EnsurePurchaseCoinVfxCanvas()
    {
        if (purchaseCoinVfxCanvas != null && purchaseCoinVfxRunner != null)
            return;

        GameObject canvasObject = new GameObject("Purchase Coin Burst Canvas");
        canvasObject.transform.SetParent(transform, false);

        purchaseCoinVfxCanvas = canvasObject.AddComponent<Canvas>();
        purchaseCoinVfxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        purchaseCoinVfxCanvas.overrideSorting = true;
        purchaseCoinVfxCanvas.sortingOrder = 350;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        purchaseCoinVfxRunner = canvasObject.AddComponent<CoroutineRunner>();
    }

    private Vector2 GetCoinsTextPositionOnPurchaseCanvas()
    {
        RectTransform canvasRect = purchaseCoinVfxCanvas.transform as RectTransform;
        Camera sourceCamera = null;
        Canvas sourceCanvas = coinsText.GetComponentInParent<Canvas>();

        if (sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            sourceCamera = sourceCanvas.worldCamera != null ? sourceCanvas.worldCamera : Camera.main;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, coinsText.rectTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private IEnumerator AnimatePurchaseCoin(Vector2 origin, Vector2 endPosition, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        Image coin = CreatePurchaseCoinImage(origin);
        RectTransform coinRect = coin.rectTransform;
        Color startColor = coin.color;
        float elapsed = 0f;

        while (elapsed < purchaseCoinBurstDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / purchaseCoinBurstDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector2 position = Vector2.LerpUnclamped(origin, endPosition, eased);
            position.y += Mathf.Sin(t * Mathf.PI) * purchaseCoinArcHeight;

            coinRect.anchoredPosition = position;
            coinRect.localScale = Vector3.one * Mathf.Lerp(purchaseCoinStartScale, purchaseCoinEndScale, t);

            float alpha = 1f - Mathf.SmoothStep(0.45f, 1f, t);
            coin.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * alpha);
            coinRect.Rotate(0f, 0f, 420f * Time.unscaledDeltaTime);

            yield return null;
        }

        Destroy(coin.gameObject);
    }

    private Image CreatePurchaseCoinImage(Vector2 origin)
    {
        GameObject coinObject = new GameObject("Purchase Coin");
        coinObject.transform.SetParent(purchaseCoinVfxCanvas.transform, false);

        Image coin = coinObject.AddComponent<Image>();
        coin.sprite = purchaseCoinSprite != null ? purchaseCoinSprite : GetGeneratedPurchaseCoinSprite();
        coin.color = purchaseCoinColor;
        coin.raycastTarget = false;

        RectTransform coinRect = coin.rectTransform;
        coinRect.anchorMin = new Vector2(0.5f, 0.5f);
        coinRect.anchorMax = new Vector2(0.5f, 0.5f);
        coinRect.pivot = new Vector2(0.5f, 0.5f);
        coinRect.sizeDelta = purchaseCoinSize;
        coinRect.anchoredPosition = origin;
        coinRect.localScale = Vector3.one * purchaseCoinStartScale;
        coinRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        return coin;
    }

    private Sprite GetGeneratedPurchaseCoinSprite()
    {
        if (generatedPurchaseCoinSprite != null)
            return generatedPurchaseCoinSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated Purchase Coin";

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;
        Color rim = new Color(1f, 0.55f, 0.08f, 1f);
        Color face = new Color(1f, 0.86f, 0.18f, 1f);
        Color highlight = new Color(1f, 0.97f, 0.58f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalized = distance / radius;

                if (normalized > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                Color color = normalized > 0.76f ? rim : Color.Lerp(highlight, face, normalized);
                float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), center + new Vector2(-11f, 12f)) / 18f);
                color = Color.Lerp(color, highlight, shine * 0.45f);
                color.a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.94f, 1f, normalized));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        generatedPurchaseCoinSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        generatedPurchaseCoinSprite.name = "Generated Purchase Coin";
        return generatedPurchaseCoinSprite;
    }

    private sealed class CoroutineRunner : MonoBehaviour
    {
    }

    private KeyCode[] konamiCode = new KeyCode[]
    {
     KeyCode.UpArrow,
     KeyCode.UpArrow,
     KeyCode.DownArrow,
     KeyCode.DownArrow,
     KeyCode.LeftArrow,
     KeyCode.RightArrow,
     KeyCode.LeftArrow,
     KeyCode.RightArrow
    };
    private void Update()
    {
        CheckKonamiCode();
    }
    void CheckKonamiCode()
    {
        if (Input.GetKeyDown(konamiCode[konamiIndex]))
        {
            konamiIndex++;

            if (konamiIndex >= konamiCode.Length)
            {
                ActivateKonamiReward();
                konamiIndex = 0;
            }
        }
        else if (Input.anyKeyDown)
        {
            konamiIndex = 0;
        }
    }
    void ActivateKonamiReward()
    {
        coins += 100;
        UpdateCoinsUI();
        ShowFloatingCoins(100);
    }
}
