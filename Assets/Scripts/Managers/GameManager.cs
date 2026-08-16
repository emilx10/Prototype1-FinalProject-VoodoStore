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

    [Header("Sell Offer Rewards")]
    public int safeSellMin;
    public int safeSellMax;
    public int fairSellMin;
    public int fairSellMax;
    public int riskySellMin;
    public int riskySellMax;
    public int temptFateReward;

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

    [Header("Sell Offer Rewards")]
    public int safeSellMin;
    public int safeSellMax;
    public int fairSellMin;
    public int fairSellMax;
    public int riskySellMin;
    public int riskySellMax;
    public int temptFateReward;
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
    private const float ShopOpenVolume = 0.3f;
    private const float PurchaseVolumeMultiplier = 0.5f;
    private const float ItemPurchaseVolume = 0.3f;
    private const float MysteriousVolumeMultiplier = 0.4f;
    private const int ProductShopPrice = 80;
    private static readonly string[] ProductShopRecipeNames =
    {
        "Voodoo doll",
        "Lie-detecting head",
        "Crystal ball",
        "Good hair day charm",
        "Rings of pain relief",
        "Happyness tiara",
        "Love Potion",
        "Sleep Potion",
        "Bad Luck Potion"
    };
    private const float BookOpenVolume = 0.5f;
    private const string UltimatePotionRecipeName = "ultimate potion";
    private const int CheatMenuSortingOrder = 32100;
    private const int SellItemsVisiblePerPage = 3;
    private const int SellItemsCanvasSortingOrder = 63;

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

    [Header("Sell Phase Customers")]
    [SerializeField] private List<GameObject> sellPhaseCustomers = new List<GameObject>();
    [SerializeField] private bool autoFindSellPhaseCustomers = true;
    [SerializeField] private Button sellItemsPreviousPageButton;
    [SerializeField] private Button sellItemsNextPageButton;
    [SerializeField] private Vector2 sellItemsPreviousArrowPosition = new Vector2(-245f, -405f);
    [SerializeField] private Vector2 sellItemsNextArrowPosition = new Vector2(460f, -405f);
    [SerializeField] private Vector2 sellItemsArrowSize = new Vector2(80f, 80f);
    [SerializeField] private Vector3 sellItemsArrowScale = Vector3.one;
    [SerializeField] private bool overrideSellItemButtonSize = true;
    [SerializeField] private Vector2 sellItemButtonSize = new Vector2(120f, 120f);
    [SerializeField] private bool overrideSellItemsParentRect = true;
    [SerializeField] private Vector2 sellItemsParentPosition = new Vector2(-409f, -395.58f);
    [SerializeField] private Vector2 sellItemsParentSize = new Vector2(443.57f, 149.58f);

    [Header("Crafting Triangle Layout")]
    [SerializeField] private Vector2 triangleLeftTop;
    [SerializeField] private Vector2 triangleRightTop;
    [SerializeField] private Vector2 triangleCenterBottom;

    [Header("Junk Settings")]
    [SerializeField] private string junkItemName = "Junk";
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
    [SerializeField] private int newDayCurrencyReward = 20;

    [Header("Sell Confirmation UI")]
    [SerializeField] private GameObject sellConfirmPanel;
    [SerializeField] private Slider priceSlider;
    [SerializeField] private TMP_Text priceValueText;
    [SerializeField] private TMP_Text sellItemNameText;
    [SerializeField] private Button confirmSellButton;
    [SerializeField] private int sellConfirmPanelSortingOrder = 60;

    [Header("Sell Offer Settings")]
    [SerializeField, Range(0f, 1f)] private float safeOfferChance = 1f;
    [SerializeField, Range(0f, 1f)] private float fairOfferChance = 0.75f;
    [SerializeField, Range(0f, 1f)] private float riskyOfferChance = 0.55f;
    [SerializeField, Range(0f, 1f)] private float temptFateOfferChance = 0.2f;
    [SerializeField] private Color safeOfferColor = new Color32(0x6F, 0xAF, 0x7A, 0xFF);
    [SerializeField] private Color fairOfferColor = new Color32(0x6F, 0x8F, 0xB8, 0xFF);
    [SerializeField] private Color riskyOfferColor = new Color32(0xC7, 0x6A, 0x57, 0xFF);
    [SerializeField] private Color temptFateOfferColor = new Color32(0x8C, 0x63, 0xB8, 0xFF);
    [SerializeField] private int ingredientSellReward = 3;
    [SerializeField] private int junkSafeReward = 2;
    [SerializeField, Range(0f, 1f)] private float junkRiskyChance = 0.6f;
    [SerializeField] private int junkRiskyMinReward = 0;
    [SerializeField] private int junkRiskyMaxReward = 5;
    [SerializeField] private GameObject sellOfferChoicesRoot;

    private enum SellOfferType { Safe, Fair, Risky, TemptFate }
    private readonly Dictionary<SellOfferType, Button> sellOfferButtons = new Dictionary<SellOfferType, Button>();
    private bool sellOfferRuntimeListenersBound;

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
    [Tooltip("Local offset from the spawn point when coins are deducted by a purchase.")]
    [SerializeField] private Vector2 floatingCoinBuyOffset = new Vector2(0f, -20f);
    [Tooltip("Local offset from the spawn point when coins are added by a sale.")]
    [SerializeField] private Vector2 floatingCoinSellOffset = new Vector2(0f, 20f);
    [SerializeField] private Vector2 floatingCoinTextSize = new Vector2(200f, 50f);
    [SerializeField] private TMP_FontAsset floatingCoinFont;
    [SerializeField, Min(1f)] private float floatingCoinFontSize = 40f;
    [SerializeField] private Color floatingCoinAddedColor = Color.yellow;
    [SerializeField] private Color floatingCoinDeductedColor = Color.red;
    [Tooltip("Canvas sorting order used by floating coin text so it renders above shop and sell panels.")]
    [SerializeField] private int floatingCoinSortingOrder = 20000;
    [SerializeField, Min(0f)] private float floatingCoinFloatSpeed = 50f;
    [SerializeField, Min(0.05f)] private float floatingCoinLifetime = 1f;
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
    [Header("Ultimate Potion Recipe Highlight")]
    [SerializeField] private Color ultimatePotionGlowColor = new Color(1f, 0.05f, 0.02f, 0.95f);
    [SerializeField, Range(0f, 8f)] private float ultimatePotionGlowIntensity = 2.5f;
    [SerializeField, Range(0f, 24f)] private float ultimatePotionGlowSpread = 7f;

    public static UnityAction<Sprite> OnItemBought;
    public static UnityAction OnIngredientPurchased;
    public static UnityAction OnSuccessfulMerge;
    public static UnityAction OnFailedMerge;
    public static UnityAction <bool> OnItemSold;
    public static UnityAction<string, ItemCategory> OnItemAdded;


    private InventoryItem pendingSellItem;
    private List<InventoryItem> inventory = new List<InventoryItem>();
    private List<InventoryItem> selectedCraftingItems = new List<InventoryItem>();
    private int konamiIndex = 0;
    private Dictionary<MarketItem, int> marketStock = new Dictionary<MarketItem, int>();
    private readonly List<MarketItem> productShopItems = new List<MarketItem>();
    private Market currentMarket;
    private bool isMergeAnimationPlaying = false;
    private bool craftingExitRequired;
    private bool sellPromptUnlockedByMergeScreen;
    private bool preserveSellPromptOnNextOpenSell;
    private GameObject activeSellPhaseCustomer;
    private int sellItemsPageIndex;
    private Canvas purchaseCoinVfxCanvas;
    private CoroutineRunner purchaseCoinVfxRunner;
    private Sprite generatedPurchaseCoinSprite;
    private Sprite sellItemsArrowSprite;


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
    [SerializeField] private string gameWonText = "YOU WON!";
    [SerializeField] private Color gameWonTextColor = new Color(0.72f, 1f, 0.78f, 1f);
    [SerializeField] private string gameLostText = "YOU LOST";
    [SerializeField] private Color gameLostTextColor = new Color(0.95f, 0.16f, 0.12f, 1f);
    [SerializeField] private float gameOverTextFontSize = 142f;
    [SerializeField] private string playAgainButtonText = "Play Again";
    [SerializeField] private Color playAgainButtonColor = new Color(0.12f, 0.02f, 0.02f, 0.92f);
    [SerializeField] private Color playAgainButtonTextColor = Color.white;

    [Header("Cheat Menu")]
    [SerializeField] private GameObject cheatMenuCanvasRoot;
    [SerializeField] private GameObject cheatMenuRoot;
    [SerializeField] private Button cheatAddCoinsButton;
    [SerializeField] private Button cheatLetMeWinButton;
    [SerializeField] private Button cheatIWantToLoseButton;
    [SerializeField] private int cheatCoinsAmount = 500;
    [SerializeField] private string cheatAddCoinsButtonText = "ADD 500 COINS";
    [SerializeField] private string cheatLetMeWinButtonText = "LET ME WIN";
    [SerializeField] private string cheatIWantToLoseButtonText = "I WANT TO LOSE";
    [SerializeField] private string cheatWinPotionName = "Ultimate Potion";
    [SerializeField] private string cheatWinPotionDescription = "A forbidden potion said to return a soul to the living world.";

    [Header("Day 20 Outcome Cutscenes")]
    [SerializeField] private GameObject day20CutsceneCanvasRoot;
    [SerializeField] private GameObject day20WinCutsceneRoot;
    [SerializeField] private GameObject day20WinPotionCloseupRoot;
    [SerializeField] private GameObject day20WinGraveSpillRoot;
    [SerializeField] private GameObject day20WinVivianShopRoot;
    [SerializeField] private GameObject day20LoseCutsceneRoot;
    [SerializeField] private float day20CutsceneFadeDuration = 0.55f;
    [SerializeField] private float day20CutsceneHoldDuration = 4.5f;
    [SerializeField] private float day20WinPotionCloseupHoldDuration = 3f;
    [SerializeField] private float day20WinGraveSpillHoldDuration = 5f;
    [SerializeField] private float day20WinVivianShopHoldDuration = 7f;
    [SerializeField] private float day20WinStepFadeDuration = 0.45f;
    [SerializeField] private string day20WinUnknownIdentityText = "Identity of revived subject: Unknown";
    [SerializeField] private Sprite day20WinVivianShopBackgroundSprite;
    [SerializeField] private Sprite day20WinVivianShopNewspaperSprite;
    [SerializeField] private Vector2 day20WinVivianShopNewspaperPosition = new Vector2(520f, -10f);
    [SerializeField] private Vector2 day20WinVivianShopNewspaperSize = new Vector2(378f, 394f);
    [SerializeField] private float day20WinVivianShopNewspaperRotation = -1.5f;
    [SerializeField] private AudioClip day20WinAmbience;
    [SerializeField] private AudioClip day20LoseAmbience;
    [SerializeField, Range(0f, 1f)] private float day20AmbienceVolume = 0.7f;
    [Header("Day 20 Lose Grave Zoom")]
    [SerializeField] private Rect day20LoseStartView = new Rect(0.075f, 0.115f, 0.42f, 0.42f);
    [SerializeField] private Rect day20LoseEndView = new Rect(0f, 0f, 1f, 1f);
    [SerializeField] private float day20LoseZoomOutDuration = 4.2f;
    [SerializeField] private Color day20LoseTint = new Color(0.58f, 0.64f, 0.78f, 1f);
    [Tooltip("Any of these inventory item names count as the resurrection potion for the day 20 ending.")]
    [SerializeField] private string[] resurrectionPotionNames =
    {
        "Resurrection Potion",
        "Ressuruction Potion",
        "Resurrection Elixir",
        "Resurrection Elixar",
        "Ressuruction Elixir",
        "Ressuruction Elixar",
        "Ultimate Potion"
    };

    private bool isEndingDay;
    private Canvas dayTransitionCanvas;
    private AudioSource day20CutsceneAudioSource;
    private RectTransform sellPanelRightUiRect;
    private Image sellPanelRightUiImage;
    private RectTransform sellPanelInventoryButtonRect;
    private Image sellPanelInventoryButtonImage;
    private Sprite sellerRightUiSprite;
    private GameObject brewButton;
    private bool brewAvailableForCurrentPhase;
    private TMP_Text phaseTitleText;

    public Sprite familyMarketInventoryIcon;

    public List<InventoryItem> GetInventoryItems()
    {
        return inventory;
    }

    public ObjectiveManager objectiveManager;
    public int CurrentDay => currentDay;
    public ButtonBreather MarketAttentionBreather =>
        endDayButton != null ? endDayButton.GetComponent<ButtonBreather>() : null;

    public void OpenKnownRecipes()
    {
        if (knownRecipesPanel == null)
            return;

        bool wasClosed = !knownRecipesPanel.activeSelf;
        knownRecipesPanel.SetActive(true);
        if (wasClosed && ad != null)
            ad.PlaySfx(BookOpenVolume, SFX.BookOpen, 1f);

        PopulateKnownRecipesUI();
    }

    public void CloseKnownRecipes()
    {
        if (knownRecipesPanel != null)
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
            bookCanvasRoot.SetActive(false);

        if (bookCanvas != null)
        {
            bookCanvas.overrideSorting = true;
            bookCanvas.sortingOrder = 0;
        }

        if (bookCanvasGroup != null)
        {
            bookCanvasGroup.alpha = 0f;
            bookCanvasGroup.interactable = false;
            bookCanvasGroup.blocksRaycasts = false;
        }

        if (bookRoot != null)
            bookRoot.SetActive(false);

        if (knownRecipesOpenButton != null)
        {
            knownRecipesOpenButton.interactable = false;

            Graphic hitTarget = knownRecipesOpenButton.targetGraphic;
            if (hitTarget != null)
            {
                hitTarget.raycastTarget = false;
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

#if UNITY_EDITOR
        if (!Application.isPlaying && sellConfirmPanel != null)
        {
            UnityEditor.EditorApplication.delayCall -= BuildSellOfferChoicesEditablePreview;
            UnityEditor.EditorApplication.delayCall += BuildSellOfferChoicesEditablePreview;
        }
#endif

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
            Outline recipeBackgroundOutline = recipeBackground.GetComponent<Outline>();
            if (recipeBackgroundOutline != null)
                recipeBackgroundOutline.enabled = false;

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
                ApplyUltimatePotionGlow(resultIcon, recipeDiscovered ? recipe : null);

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

    private void ApplyUltimatePotionGlow(Graphic targetGraphic, Recipe recipe)
    {
        if (targetGraphic == null)
            return;

        Outline glow = targetGraphic.GetComponent<Outline>();
        if (glow != null)
            glow.enabled = false;

        UltimatePotionAuraUtility.Apply(
            targetGraphic as Image,
            ShouldHighlightUltimatePotionRecipe(recipe),
            GetUltimatePotionGlowColor(),
            GetUltimatePotionGlowIntensity(),
            GetUltimatePotionGlowSpread());
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

        if (recipes != null)
        {
            foreach (Recipe recipe in recipes)
            {
                if (recipe == null || NormalizeName(recipe.potionName) != normalizedIngredient)
                    continue;

                if (recipe.category == ItemCategory.Potion)
                    return new Color(0.8f, 0.12f, 0.12f, 1f);
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

        if (knownRecipesPanel != null && knownRecipesPanel.activeInHierarchy)
            PopulateKnownRecipesUI();

        FamilyMarketUI.RefreshIfVisible();
        SellPanelRightUIBinder.RefreshVisible();
        NotifyObjectiveRecipeDiscoveryChanged();
        return true;
    }

    public bool TryDiscoverProductRecipe(string productName, out bool revealedSomething)
    {
        revealedSomething = false;
        string normalizedProductName = NormalizeName(productName);
        Recipe productRecipe = null;

        foreach (Recipe recipe in recipes)
        {
            if (recipe != null &&
                recipe.category == ItemCategory.Potion &&
                NormalizeName(recipe.potionName) == normalizedProductName)
            {
                productRecipe = recipe;
                break;
            }
        }

        if (productRecipe == null)
            return false;

        if (!IsRecipeDiscovered(productRecipe))
        {
            for (int ingredientIndex = 0; ingredientIndex < productRecipe.ingredients.Count; ingredientIndex++)
            {
                revealedSomething |= discoveredRecipeIngredientSlots.Add(
                    GetRecipeIngredientSlotKey(productRecipe, ingredientIndex));
            }
        }

        if (revealedSomething)
        {
            if (knownRecipesPanel != null && knownRecipesPanel.activeInHierarchy)
                PopulateKnownRecipesUI();

            FamilyMarketUI.RefreshIfVisible();
            SellPanelRightUIBinder.RefreshVisible();
            NotifyObjectiveRecipeDiscoveryChanged();
        }

        return true;
    }

    private void DiscoverRecipe(Recipe recipe)
    {
        discoveredRecipes.Add(NormalizeName(recipe.potionName));

        if (knownRecipesPanel != null && knownRecipesPanel.activeInHierarchy)
            PopulateKnownRecipesUI();

        FamilyMarketUI.RefreshIfVisible();
        SellPanelRightUIBinder.RefreshVisible();
        NotifyObjectiveRecipeDiscoveryChanged();
    }

    private void NotifyObjectiveRecipeDiscoveryChanged()
    {
        ObjectiveManager manager = objectiveManager != null
            ? objectiveManager
            : FindFirstObjectByType<ObjectiveManager>();
        manager?.NotifyRecipeDiscoveryChanged();
    }

    public bool IsRecipeDiscovered(Recipe recipe)
    {
        return recipe != null && discoveredRecipes.Contains(NormalizeName(recipe.potionName));
    }

    public bool ShouldHighlightUltimatePotionRecipe(Recipe recipe)
    {
        return recipe != null && NormalizeName(recipe.potionName) == UltimatePotionRecipeName;
    }

    public Color GetUltimatePotionGlowColor()
    {
        Color glowColor = ultimatePotionGlowColor;
        glowColor.a = Mathf.Clamp01(glowColor.a);
        return glowColor;
    }

    public float GetUltimatePotionGlowIntensity()
    {
        return ultimatePotionGlowIntensity;
    }

    public float GetUltimatePotionGlowSpread()
    {
        return ultimatePotionGlowSpread;
    }

    public bool IsRecipeIngredientSlotDiscovered(Recipe recipe, int ingredientIndex)
    {
        if (recipe == null || ingredientIndex < 0 || ingredientIndex >= recipe.ingredients.Count)
            return false;

        return IsRecipeDiscovered(recipe) ||
            discoveredRecipeIngredientSlots.Contains(GetRecipeIngredientSlotKey(recipe, ingredientIndex));
    }

    public Sprite GetKnownRecipeIngredientIcon(string ingredientName)
    {
        return GetIconByNameInsensitive(ingredientName);
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
    private void OnEnable()
    {
        DayNightCycleUI.PhaseChanged += UpdatePhaseTitle;
        UpdatePhaseTitle(DayNightCycleUI.CurrentPhase);
    }

    private void OnDisable()
    {
        DayNightCycleUI.PhaseChanged -= UpdatePhaseTitle;

#if UNITY_EDITOR
        // OnValidate can queue an editor preview immediately before entering
        // Play Mode. Remove it when this scene object is disabled/destroyed so
        // the delayed callback cannot target a stale SerializedObject.
        UnityEditor.EditorApplication.delayCall -= BuildSellOfferChoicesEditablePreview;
#endif
    }

    void Start()
    {
        // The scene's serialized AudioManager is destroyed when a persistent
        // singleton already exists after Play Again. Always use the survivor.
        if (AudioManager.Instance != null)
            ad = AudioManager.Instance;

        FTUEManager.RegisterMarketControl(endDayButton, MarketAttentionBreather);

        ApplyDayNightCycleUISettings();
        DayNightCycleUI.SetPhase(dayNightStartingPhase, true);
        FamilyMarketUI.Attach(this);
        EnsureFrontCanvas(itemsPanel, ShopItemsSortingOrder);
        EnsureSellConfirmPanelCanvas();
        EnsureSellPanelRightUI();
        EnsureSellItemsPaginationArrows();
        RandomizeMarketStock();
        PopulateInventoryPanel();
        UpdateCoinsUI();
        EnsureSellOfferUI();
        SetLegacySellControlsVisible(false);

        // Startup panel/tab binding can temporarily restore this child. Apply
        // the initial gameplay state last, before Unity renders the first frame.
        SetBrewButtonVisible(false);

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
        ad.PlaySfx(ShopOpenVolume, SFX.EnteredShop, 1f);

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

        EnsureProductShopItems();
        foreach (MarketItem product in productShopItems)
            marketStock[product] = 1;
    }

    private void EnsureProductShopItems()
    {
        if (productShopItems.Count > 0 || recipes == null)
            return;

        foreach (string requestedName in ProductShopRecipeNames)
        {
            Recipe matchingRecipe = null;
            string normalizedRequestedName = NormalizeName(requestedName);

            foreach (Recipe recipe in recipes)
            {
                if (recipe != null && NormalizeName(recipe.potionName) == normalizedRequestedName)
                {
                    matchingRecipe = recipe;
                    break;
                }
            }

            if (matchingRecipe == null)
            {
                Debug.LogError($"Product Shop could not find recipe definition: {requestedName}");
                continue;
            }

            productShopItems.Add(new MarketItem
            {
                itemName = matchingRecipe.potionName,
                price = ProductShopPrice,
                category = ItemCategory.Potion,
                description = string.Empty,
                icon = matchingRecipe.icon,
                minSellPrice = matchingRecipe.minSellPrice,
                maxSellPrice = matchingRecipe.maxSellPrice,
                minAmount = 1,
                maxAmount = 1
            });
        }
    }

    public List<MarketItem> GetProductShopItems()
    {
        EnsureProductShopItems();
        return productShopItems;
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
        ad.PlaySfx(vol * PurchaseVolumeMultiplier, SFX.Buying, pitch);
        ad.PlaySfx(ItemPurchaseVolume, GetItemPurchaseSfx(item.category), 1f);

        OnItemBought?.Invoke(item.icon);
        if (item.category != ItemCategory.Potion && item.category != ItemCategory.Junk)
            OnIngredientPurchased?.Invoke();
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

    private SFX GetItemPurchaseSfx(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Oils:
                return SFX.ShopOils;
            case ItemCategory.Gems:
                return SFX.ShopGems;
            case ItemCategory.Herbs:
                return SFX.ShopHerbs;
            case ItemCategory.Potion:
                return SFX.MergePotion;
            case ItemCategory.Junk:
                return SFX.JunkMerge;
            default:
                return SFX.Buying;
        }
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
        {
            marketShopButton.onClick.Invoke();
            if (marketShopButton.onClick.GetPersistentEventCount() > 0)
                return;
        }

        OpenSell();
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
                if (sellPanelRightUiImage.sprite == null)
                    sellPanelRightUiImage.sprite = LoadSellerRightUiSprite();

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

        SellPanelRightUIBinder binder = sellPanelRightUiRect != null
            ? sellPanelRightUiRect.GetComponent<SellPanelRightUIBinder>()
            : null;
        if (binder != null)
        {
            binder.SetGameManager(this);
            binder.Refresh();
        }

        EnsureSellPanelCraftButtonClickable();
    }

    private void EnsureSellPanelCraftButtonClickable()
    {
        if (sellPanel == null)
            return;

        Transform craftButtonTransform = FindDeepChild(sellPanel.transform, "CraftItemsButton");
        if (craftButtonTransform == null)
            return;

        Button craftButton = craftButtonTransform.GetComponent<Button>();
        if (craftButton == null)
            return;

        Canvas craftCanvas = craftButtonTransform.GetComponent<Canvas>();
        if (craftCanvas == null)
            craftCanvas = craftButtonTransform.gameObject.AddComponent<Canvas>();

        craftCanvas.overrideSorting = true;

        int minimumSortingOrder = 151;
        Canvas rightPanelCanvas = sellPanelRightUiRect != null ? sellPanelRightUiRect.GetComponent<Canvas>() : null;
        if (rightPanelCanvas != null && rightPanelCanvas.overrideSorting)
            minimumSortingOrder = Mathf.Max(minimumSortingOrder, rightPanelCanvas.sortingOrder + 10);

        if (craftCanvas.sortingOrder < minimumSortingOrder)
            craftCanvas.sortingOrder = minimumSortingOrder;

        if (craftButtonTransform.GetComponent<GraphicRaycaster>() == null)
            craftButtonTransform.gameObject.AddComponent<GraphicRaycaster>();

        Graphic targetGraphic = craftButton.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.raycastTarget = true;

        craftButton.interactable = true;
    }

    private void SetBrewButtonVisible(bool visible)
    {
        brewAvailableForCurrentPhase = visible;
        ApplyBrewButtonVisibility();
    }

    private void ApplyBrewButtonVisibility()
    {
        if (brewButton == null && sellPanel != null)
        {
            Transform brewButtonTransform = FindDeepChild(sellPanel.transform, "CraftItemsButton");
            if (brewButtonTransform != null)
                brewButton = brewButtonTransform.gameObject;
        }

        if (brewButton != null)
            brewButton.SetActive(brewAvailableForCurrentPhase);
    }

    public void SetBrewButtonVisibleForGameplayPhase(bool visible)
    {
        SetBrewButtonVisible(visible);
    }

    public void RefreshBrewButtonVisibility()
    {
        ApplyBrewButtonVisibility();
    }

#if UNITY_EDITOR
    [ContextMenu("Build / Refresh Sell Panel Right UI Copy")]
    private void BuildRefreshSellPanelRightUICopy()
    {
        if (sellPanel == null)
        {
            Debug.LogWarning("Cannot copy the Seller Right UI Block because SellPanel is not assigned.");
            return;
        }

        FamilyMarketUI familyMarketUI = FindFirstObjectByType<FamilyMarketUI>(FindObjectsInactive.Include);
        if (familyMarketUI != null)
            familyMarketUI.BuildEditablePreview();

        Transform sourceBlock = familyMarketUI != null
            ? FindDeepChild(familyMarketUI.transform, "Seller Right UI Block")
            : null;
        if (sourceBlock == null)
        {
            Debug.LogWarning("Cannot find Family UI Seller Right UI Block to copy.");
            return;
        }

        Transform existingBlock = sellPanel.transform.Find("Sell Panel Right UI Block");
        if (existingBlock != null)
            DestroyImmediate(existingBlock.gameObject);

        GameObject copy = Instantiate(sourceBlock.gameObject, sellPanel.transform, false);
        copy.name = "Sell Panel Right UI Block";

        RectTransform sourceRect = sourceBlock as RectTransform;
        RectTransform copyRect = copy.GetComponent<RectTransform>();
        if (sourceRect != null && copyRect != null)
        {
            copyRect.anchorMin = sourceRect.anchorMin;
            copyRect.anchorMax = sourceRect.anchorMax;
            copyRect.pivot = sourceRect.pivot;
            copyRect.anchoredPosition = sourceRect.anchoredPosition;
            copyRect.sizeDelta = sourceRect.sizeDelta;
            copyRect.localRotation = sourceRect.localRotation;
            copyRect.localScale = sourceRect.localScale;
        }

        SellPanelRightUIBinder binder = copy.GetComponent<SellPanelRightUIBinder>();
        if (binder == null)
            binder = copy.AddComponent<SellPanelRightUIBinder>();
        binder.SetGameManager(this);

        Transform oldInventoryButton = sellPanel.transform.Find("Sell Panel Inventory Button");
        if (oldInventoryButton != null)
            oldInventoryButton.gameObject.SetActive(false);

        sellPanelRightUiRect = copyRect;
        sellPanelRightUiImage = copy.GetComponent<Image>();
        sellPanelInventoryButtonRect = null;
        sellPanelInventoryButtonImage = null;

        UnityEditor.EditorUtility.SetDirty(copy);
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

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
        sellPromptUnlockedByMergeScreen = false;

        if (endDayButton != null)
            endDayButton.interactable = false;

        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        sellPanel.SetActive(true);

        craftingPanel.SetActive(true);
        ActivateCraftingContextBlocker();
        SetBrewButtonVisible(false);

        selectedCraftingItems.Clear();

        RefreshCraftingUI();
        RefreshSelectedItemsUI();
    }

    private void ActivateCraftingContextBlocker()
    {
        if (craftingPanel == null)
            return;

        ContextBlocker fallback = null;
        ContextBlocker[] blockers = FindObjectsByType<ContextBlocker>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < blockers.Length; i++)
        {
            ContextBlocker blocker = blockers[i];
            if (blocker == null)
                continue;

            if (blocker.name == "SellBlocker")
                fallback = blocker;

            if (!blocker.TargetsContext(craftingPanel))
                continue;

            blocker.AssignGameManagerIfMissing(this);
            blocker.gameObject.SetActive(true);
            return;
        }

        if (fallback != null)
        {
            fallback.AssignGameManagerIfMissing(this);
            fallback.gameObject.SetActive(true);
        }
    }

    void RefreshCraftingUI()
    {
        ClearChildren(craftingItemsParent);

        foreach (InventoryItem item in inventory)
        {
            if (!CanUseAsCraftingIngredient(item)) continue;

            GameObject btn = Instantiate(buttonPrefab, craftingItemsParent);

            // Assign tooltip
            var tooltip = btn.GetComponent<ItemHoverTooltip>();
            tooltip.inventoryItem = item;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                bool showProductQuantity = item.category == ItemCategory.Potion;
                txt.text = "x" + item.count;
                txt.gameObject.SetActive(showProductQuantity);
            }

            ApplyCategoryStyle(btn, item.category);

            Image iconImage = btn.transform.Find("Icon").GetComponent<Image>();
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;

            btn.GetComponent<Button>().onClick.AddListener(() => SelectCraftingItem(item));
        }
    }

    private bool CanUseAsCraftingIngredient(InventoryItem item)
    {
        if (item == null || item.count <= 0)
            return false;

        // Finished Products use the same selection, consumption, and recipe
        // matching path as Ingredients. Products that are not part of a valid
        // three-item recipe naturally fall through to the existing Junk result.
        if (item.category == ItemCategory.Potion)
            return true;

        if (!IsRecipeItem(item.itemName))
            return true;

        return IsIngredientInAnyRecipe(item.itemName);
    }

    private bool IsIngredientInAnyRecipe(string itemName)
    {
        string cleanedItemName = NormalizeName(itemName);
        if (recipes == null)
            return false;

        foreach (Recipe recipe in recipes)
        {
            if (recipe == null || recipe.ingredients == null)
                continue;

            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                if (NormalizeName(recipe.ingredients[i]) == cleanedItemName)
                    return true;
            }
        }

        return false;
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
        string craftedPotionName = null;

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
                ad.PlaySfx(vol * MysteriousVolumeMultiplier, SFX.MergePotion, pitch);
                AddToInventory(recipe.potionName, recipe.category, "", recipe.icon);
                DiscoverRecipe(recipe);
                craftedSomething = true;
                craftedPotionName = recipe.potionName;
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
        UnlockSellPromptAfterMergeScreen();

        ObjectiveManager manager = FindFirstObjectByType<ObjectiveManager>();
        if (manager != null && !string.IsNullOrWhiteSpace(craftedPotionName))
            manager.CompleteBrewedPotion(craftedPotionName);

        if (!isEndingDay &&
            !string.IsNullOrWhiteSpace(craftedPotionName) &&
            IsResurrectionPotionName(craftedPotionName))
        {
            StartCoroutine(ShowImmediateWinCutsceneAfterMergeRoutine());
        }
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
        UnlockSellPromptAfterMergeScreen();
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

        if (preserveSellPromptOnNextOpenSell)
        {
            sellPromptUnlockedByMergeScreen = true;
            preserveSellPromptOnNextOpenSell = false;
        }
        else
        {
            sellPromptUnlockedByMergeScreen = false;
        }

        DayNightCycleUI.SetPhase(DayNightPhase.Evening);

        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(true);
        HideSellPhaseCustomers();
        SetBrewButtonVisible(true);
        EnsureSellPanelRightUI();
        EnsureSellConfirmPanelCanvas();
        EnsureSellItemsPaginationArrows();
        ReturnCraftingItemsToInventory();
        RefreshSellUI();
    }

    private void UpdatePhaseTitle(DayNightPhase phase)
    {
        if (phaseTitleText == null && sellPanel != null)
        {
            Transform titleTransform = FindDeepChild(sellPanel.transform, "SellText");
            if (titleTransform != null)
                phaseTitleText = titleTransform.GetComponent<TMP_Text>();
        }

        if (phaseTitleText == null)
            return;

        bool visible;
        switch (phase)
        {
            case DayNightPhase.Evening:
                phaseTitleText.text = "Brew Potions";
                visible = true;
                break;
            case DayNightPhase.Night when sellPromptUnlockedByMergeScreen:
                phaseTitleText.text = "Sell Potions";
                visible = true;
                break;
            default:
                visible = false;
                break;
        }

        phaseTitleText.gameObject.SetActive(visible);
    }
    void SellItem(InventoryItem item, int price)
    {
        bool soldProduct = item != null && item.category == ItemCategory.Potion;
        coins += price;
        RemoveFromInventory(item.itemName);

        RefreshSellUI();
        PopulateInventoryPanel();
        SellPanelRightUIBinder.RefreshVisible();
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

        ObjectiveManager manager = FindFirstObjectByType<ObjectiveManager>();
        if (manager != null)
        {
            manager.CompleteMission(MissionType.SellItems);
            if (soldProduct)
                manager.NotifySuccessfulProductSale();
        }
        if (inventoryPanel.activeSelf)
            PopulateInventoryPanel();
    }

    public void RefreshSellUI()
    {
        ApplySellItemsParentRect();
        ClearChildren(sellItemsParent);
        EnsureSellItemsRenderInFrontOfCustomer();
        EnsureSellItemsPaginationArrows();
        ApplySellItemsParentGridSize();
        ClampSellItemsPageIndex();
        RefreshSellItemsPaginationArrowState();

        int displayedItems = 0;
        int skippedItems = 0;
        int startIndex = sellItemsPageIndex * SellItemsVisiblePerPage;
        foreach (InventoryItem item in inventory)
        {
            if (item == null || item.count <= 0)
                continue;

            if (skippedItems < startIndex)
            {
                skippedItems++;
                continue;
            }

            if (displayedItems >= SellItemsVisiblePerPage)
                break;

            GameObject btn = Instantiate(buttonPrefab, sellItemsParent);
            ApplySellItemButtonSize(btn);
            displayedItems++;

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
                tooltip.overrideText = "Already rejected today";
                AddRejectedSaleIndicator(btn.transform);
            }
            else
            {
                button.interactable = true;
                button.onClick.AddListener(() => OnSellClicked(item));
            }
        }
    }

    [ContextMenu("Build Editable Sell Item Arrows")]
    private void BuildRefreshSellItemArrows()
    {
        EnsureSellItemsPaginationArrows(true);
    }

    private void ApplySellItemButtonSize(GameObject buttonObject)
    {
        if (!overrideSellItemButtonSize || buttonObject == null)
            return;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = sellItemButtonSize;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = buttonObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = sellItemButtonSize.x;
        layoutElement.preferredHeight = sellItemButtonSize.y;
    }

    private void ApplySellItemsParentRect()
    {
        if (!overrideSellItemsParentRect || sellItemsParent == null)
            return;

        RectTransform rect = sellItemsParent as RectTransform;
        if (rect == null)
            return;

        rect.anchoredPosition = sellItemsParentPosition;
        rect.sizeDelta = sellItemsParentSize;
    }

    private void ApplySellItemsParentGridSize()
    {
        if (!overrideSellItemButtonSize || sellItemsParent == null)
            return;

        GridLayoutGroup grid = sellItemsParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
            grid.cellSize = sellItemButtonSize;
    }

    private void EnsureSellItemsPaginationArrows(bool allowCreate = false)
    {
        if (sellPanel == null)
            return;

        Transform arrowParent = sellItemsParent != null && sellItemsParent.parent != null
            ? sellItemsParent.parent
            : sellPanel.transform;

        sellItemsPreviousPageButton = EnsureSellItemsPageArrow(
            sellItemsPreviousPageButton,
            "Sell Items Arrow Left",
            arrowParent,
            sellItemsPreviousArrowPosition,
            true,
            allowCreate);

        sellItemsNextPageButton = EnsureSellItemsPageArrow(
            sellItemsNextPageButton,
            "Sell Items Arrow Right",
            arrowParent,
            sellItemsNextArrowPosition,
            false,
            allowCreate);

        WireSellItemsPaginationArrows();
        RefreshSellItemsPaginationArrowState();
    }

    private Button EnsureSellItemsPageArrow(Button button, string objectName, Transform parent, Vector2 position, bool flipHorizontal, bool allowCreate)
    {
        bool createdArrow = false;
        if (button == null)
        {
            Transform existing = parent != null ? parent.Find(objectName) : null;
            if (existing != null)
                button = existing.GetComponent<Button>();
        }

        if (button == null && allowCreate)
        {
            GameObject arrowObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            arrowObject.transform.SetParent(parent, false);

            Image image = arrowObject.GetComponent<Image>();
            image.sprite = LoadSellItemsArrowSprite();
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.color = Color.white;

            button = arrowObject.GetComponent<Button>();
            button.targetGraphic = image;
            createdArrow = true;
        }

        if (button == null)
            return null;

        Image arrowImage = button.GetComponent<Image>();
        if (arrowImage != null && arrowImage.sprite == null)
        {
            arrowImage.sprite = LoadSellItemsArrowSprite();
            arrowImage.preserveAspect = true;
            arrowImage.raycastTarget = true;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (createdArrow && rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sellItemsArrowSize;
            rect.localScale = new Vector3(
                Mathf.Abs(sellItemsArrowScale.x) * (flipHorizontal ? -1f : 1f),
                sellItemsArrowScale.y,
                sellItemsArrowScale.z);
        }

        EnsureFrontCanvas(button.gameObject, SellItemsCanvasSortingOrder);
        return button;
    }

    private void WireSellItemsPaginationArrows()
    {
        WireSellItemsPageArrow(sellItemsPreviousPageButton, PreviousSellItemsPage);
        WireSellItemsPageArrow(sellItemsNextPageButton, NextSellItemsPage);
    }

    private static void WireSellItemsPageArrow(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void PreviousSellItemsPage()
    {
        if (!IsSellItemsPaginationVisible())
            return;

        sellItemsPageIndex--;
        ClampSellItemsPageIndex();
        RefreshSellUI();
    }

    private void NextSellItemsPage()
    {
        if (!IsSellItemsPaginationVisible())
            return;

        sellItemsPageIndex++;
        ClampSellItemsPageIndex();
        RefreshSellUI();
    }

    private void RefreshSellItemsPaginationArrowState()
    {
        int maxPageIndex = GetMaxSellItemsPageIndex();
        bool hasMultiplePages = maxPageIndex > 0;
        bool visible = hasMultiplePages && IsSellItemsPaginationVisible();

        if (sellItemsPreviousPageButton != null)
        {
            sellItemsPreviousPageButton.gameObject.SetActive(visible);
            sellItemsPreviousPageButton.interactable = visible && sellItemsPageIndex > 0;
        }

        if (sellItemsNextPageButton != null)
        {
            sellItemsNextPageButton.gameObject.SetActive(visible);
            sellItemsNextPageButton.interactable = visible && sellItemsPageIndex < maxPageIndex;
        }
    }

    private bool IsSellItemsPaginationVisible()
    {
        return sellPanel != null &&
            sellPanel.activeInHierarchy &&
            DayNightCycleUI.CurrentPhase == DayNightPhase.Night;
    }

    private Sprite LoadSellItemsArrowSprite()
    {
        if (sellItemsArrowSprite != null)
            return sellItemsArrowSprite;

        sellItemsArrowSprite = Resources.Load<Sprite>("FamilyMarket/Arrow");
        if (sellItemsArrowSprite != null)
            return sellItemsArrowSprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>("FamilyMarket/Arrow");
        if (sprites != null && sprites.Length > 0)
            sellItemsArrowSprite = sprites[0];

        return sellItemsArrowSprite;
    }

    private void ClampSellItemsPageIndex()
    {
        sellItemsPageIndex = Mathf.Clamp(sellItemsPageIndex, 0, GetMaxSellItemsPageIndex());
    }

    private int GetMaxSellItemsPageIndex()
    {
        int sellableInventoryCount = GetVisibleSellInventoryCount();
        return Mathf.Max(0, Mathf.CeilToInt(sellableInventoryCount / (float)SellItemsVisiblePerPage) - 1);
    }

    private int GetVisibleSellInventoryCount()
    {
        int count = 0;
        foreach (InventoryItem item in inventory)
        {
            if (item != null && item.count > 0)
                count++;
        }

        return count;
    }

    private static void AddRejectedSaleIndicator(Transform sellItemTransform)
    {
        if (sellItemTransform == null || sellItemTransform.Find("Rejected Sale X") != null)
            return;

        GameObject indicatorObject = new GameObject("Rejected Sale X", typeof(RectTransform));
        indicatorObject.transform.SetParent(sellItemTransform, false);

        RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.anchoredPosition = Vector2.zero;
        RectTransform slotRect = sellItemTransform as RectTransform;
        Vector2 slotSize = slotRect != null ? slotRect.rect.size : new Vector2(141f, 105f);
        indicatorRect.sizeDelta = slotSize * 0.68f;

        TextMeshProUGUI indicator = indicatorObject.AddComponent<TextMeshProUGUI>();
        indicator.text = "X";
        indicator.enableAutoSizing = true;
        indicator.fontSizeMin = 36f;
        indicator.fontSizeMax = 96f;
        indicator.fontStyle = FontStyles.Bold;
        indicator.alignment = TextAlignmentOptions.Center;
        indicator.color = new Color(0.86f, 0.05f, 0.04f, 1f);
        indicator.raycastTarget = false;
    }

    private void UnlockSellPromptAfterMergeScreen()
    {
        sellPromptUnlockedByMergeScreen = true;
        preserveSellPromptOnNextOpenSell = true;
        UpdatePhaseTitle(DayNightCycleUI.CurrentPhase);
        if (DayNightCycleUI.CurrentPhase == DayNightPhase.Night)
            ShowRandomSellPhaseCustomer();

        if (sellPanel != null && sellPanel.activeInHierarchy)
            RefreshSellUI();
    }

    private void HideSellPhaseCustomers()
    {
        EnsureSellPhaseCustomers();
        activeSellPhaseCustomer = null;

        for (int i = 0; i < sellPhaseCustomers.Count; i++)
        {
            GameObject customer = sellPhaseCustomers[i];
            if (customer != null && customer.activeSelf)
                customer.SetActive(false);
        }

        EnsureSellItemsRenderInFrontOfCustomer();
    }

    private void ShowRandomSellPhaseCustomer()
    {
        EnsureSellPhaseCustomers();
        if (sellPhaseCustomers.Count == 0)
            return;

        int randomIndex = Random.Range(0, sellPhaseCustomers.Count);
        activeSellPhaseCustomer = sellPhaseCustomers[randomIndex];

        for (int i = 0; i < sellPhaseCustomers.Count; i++)
        {
            GameObject customer = sellPhaseCustomers[i];
            if (customer != null)
                customer.SetActive(customer == activeSellPhaseCustomer);
        }

        EnsureSellItemsRenderInFrontOfCustomer();
    }

    private void EnsureSellPhaseCustomers()
    {
        sellPhaseCustomers.RemoveAll(customer => customer == null);

        if (!autoFindSellPhaseCustomers || sellPanel == null)
            return;

        for (int i = 0; i < sellPanel.transform.childCount; i++)
        {
            Transform child = sellPanel.transform.GetChild(i);
            if (child != null && child.name.ToLowerInvariant().Contains("customer") && !sellPhaseCustomers.Contains(child.gameObject))
                sellPhaseCustomers.Add(child.gameObject);
        }
    }

    private void EnsureSellItemsRenderInFrontOfCustomer()
    {
        if (sellItemsParent == null)
            return;

        Canvas itemCanvas = sellItemsParent.GetComponent<Canvas>();
        if (itemCanvas == null)
            itemCanvas = sellItemsParent.gameObject.AddComponent<Canvas>();

        itemCanvas.overrideSorting = true;
        itemCanvas.sortingOrder = SellItemsCanvasSortingOrder;

        if (sellItemsParent.GetComponent<GraphicRaycaster>() == null)
            sellItemsParent.gameObject.AddComponent<GraphicRaycaster>();

        if (activeSellPhaseCustomer == null || activeSellPhaseCustomer.transform.parent != sellItemsParent.parent)
            return;

        int targetIndex = Mathf.Min(activeSellPhaseCustomer.transform.GetSiblingIndex() + 1, sellItemsParent.parent.childCount - 1);
        sellItemsParent.SetSiblingIndex(targetIndex);
    }

    private bool IsSellableMergedProduct(InventoryItem item)
    {
        return item != null &&
            item.count > 0 &&
            (item.category == ItemCategory.Potion || item.category == ItemCategory.Junk);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    void OnSellClicked(InventoryItem item)
    {
        TooltipManager.Instance.Hide();

        if (item.category != ItemCategory.Potion && item.category != ItemCategory.Junk)
        {
            SellItem(item, ingredientSellReward);
            return;
        }

        pendingSellItem = item;
        sellItemNameText.text = item.itemName;
        EnsureSellConfirmPanelCanvas();
        sellConfirmPanel.SetActive(true);
        ConfigureSellOffers(item);
    }

    private void EnsureSellConfirmPanelCanvas()
    {
        EnsureFrontCanvas(sellConfirmPanel, sellConfirmPanelSortingOrder);
    }

    [ContextMenu("Build / Refresh Sell Offer Choices")]
    public void BuildSellOfferChoicesEditablePreview()
    {
#if UNITY_EDITOR
        if (this == null || Application.isPlaying)
            return;
#endif

        EnsureSellOfferUI(true);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (sellOfferChoicesRoot != null)
            UnityEditor.EditorUtility.SetDirty(sellOfferChoicesRoot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void EnsureSellOfferUI()
    {
        EnsureSellOfferUI(true);
    }

    private void EnsureSellOfferUI(bool createIfMissing)
    {
        if (sellConfirmPanel == null)
            return;

        if (sellOfferChoicesRoot == null)
        {
            Transform existingRoot = FindDirectChild(sellConfirmPanel.transform, "Sell Offer Choices");
            if (existingRoot != null)
                sellOfferChoicesRoot = existingRoot.gameObject;
        }

        bool createdRoot = false;
        if (sellOfferChoicesRoot == null)
        {
            if (!createIfMissing)
                return;

            sellOfferChoicesRoot = new GameObject("Sell Offer Choices", typeof(RectTransform));
            sellOfferChoicesRoot.transform.SetParent(sellConfirmPanel.transform, false);
            createdRoot = true;
        }

        RectTransform rootRect = sellOfferChoicesRoot.GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = sellOfferChoicesRoot.AddComponent<RectTransform>();

        if (createdRoot)
        {
            // Default placement only for the first build. After that, editor changes win.
            rootRect.anchorMin = new Vector2(0.10f, 0.34f);
            rootRect.anchorMax = new Vector2(0.38f, 0.68f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        if (sellOfferButtons.Count == 4 && (!Application.isPlaying || sellOfferRuntimeListenersBound))
            return;

        sellOfferButtons.Clear();
        CreateOrBindSellOfferButton(SellOfferType.Safe, "SAFE", safeOfferColor);
        CreateOrBindSellOfferButton(SellOfferType.Fair, "FAIR", fairOfferColor);
        CreateOrBindSellOfferButton(SellOfferType.Risky, "RISKY", riskyOfferColor);
        CreateOrBindSellOfferButton(SellOfferType.TemptFate, "TEMPT FATE", temptFateOfferColor);
        sellOfferRuntimeListenersBound = Application.isPlaying;

        if (!Application.isPlaying)
        {
            SetOfferButtonLayout(SellOfferType.Safe, 0.03f, 0.53f, 0.48f, 0.97f);
            SetOfferButtonLayout(SellOfferType.Fair, 0.52f, 0.53f, 0.97f, 0.97f);
            SetOfferButtonLayout(SellOfferType.Risky, 0.03f, 0.03f, 0.48f, 0.47f);
            SetOfferButtonLayout(SellOfferType.TemptFate, 0.52f, 0.03f, 0.97f, 0.47f);
        }
    }

    private void CreateOrBindSellOfferButton(SellOfferType offerType, string label, Color color)
    {
        Transform existingButton = FindDirectChild(sellOfferChoicesRoot.transform, label);
        GameObject buttonObject = existingButton != null
            ? existingButton.gameObject
            : new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));

        buttonObject.transform.SetParent(sellOfferChoicesRoot.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null)
            buttonRect = buttonObject.AddComponent<RectTransform>();

        Image background = buttonObject.GetComponent<Image>();
        if (background == null)
            background = buttonObject.AddComponent<Image>();
        if (existingButton == null)
            background.color = color;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;

        if (Application.isPlaying)
            button.onClick.AddListener(() => ResolveSellOffer(offerType));

        Transform existingText = FindDirectChild(buttonObject.transform, "Offer Text");
        GameObject textObject = existingText != null
            ? existingText.gameObject
            : new GameObject("Offer Text", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (textRect == null)
            textRect = textObject.AddComponent<RectTransform>();
        if (existingText == null)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 5f);
            textRect.offsetMax = new Vector2(-8f, -5f);
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = textObject.AddComponent<TextMeshProUGUI>();

        // Offer labels use TMP markup for emphasis and per-line sizing.
        // Apply this to existing scene objects too, not only newly-created labels.
        text.richText = true;
        if (existingText == null)
        {
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 24f;
            text.fontStyle = FontStyles.Normal;
            text.lineSpacing = -8f;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        sellOfferButtons.Add(offerType, button);
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private void ConfigureSellOffers(InventoryItem item)
    {
        EnsureSellOfferUI();
        bool isJunk = item.category == ItemCategory.Junk || item.itemName == junkItemName;

        foreach (Button button in sellOfferButtons.Values)
            button.gameObject.SetActive(true);

        if (isJunk)
        {
            SetOfferButtonLayout(SellOfferType.Safe, 0.08f, 0.28f, 0.48f, 0.72f);
            SetOfferButtonLayout(SellOfferType.Risky, 0.52f, 0.28f, 0.92f, 0.72f);
            sellOfferButtons[SellOfferType.Fair].gameObject.SetActive(false);
            sellOfferButtons[SellOfferType.TemptFate].gameObject.SetActive(false);
            SetOfferButtonText(SellOfferType.Safe, FormatOfferText("SAFE", "100%", "2 coins"));
            SetOfferButtonText(SellOfferType.Risky, FormatOfferText("RISKY", "60%", "0-5 coins"));
            return;
        }

        SetOfferButtonLayout(SellOfferType.Safe, 0.03f, 0.53f, 0.48f, 0.97f);
        SetOfferButtonLayout(SellOfferType.Fair, 0.52f, 0.53f, 0.97f, 0.97f);
        SetOfferButtonLayout(SellOfferType.Risky, 0.03f, 0.03f, 0.48f, 0.47f);
        SetOfferButtonLayout(SellOfferType.TemptFate, 0.52f, 0.03f, 0.97f, 0.47f);

        if (!TryGetRecipe(item.itemName, out Recipe recipe))
        {
            Debug.LogError($"No recipe sell-offer data found for {item.itemName}.");
            sellConfirmPanel.SetActive(false);
            pendingSellItem = null;
            return;
        }

        SetOfferButtonText(SellOfferType.Safe, FormatOfferText("SAFE", "100%", $"{recipe.safeSellMin} - {recipe.safeSellMax} coins"));
        SetOfferButtonText(SellOfferType.Fair, FormatOfferText("FAIR", "75%", $"{recipe.fairSellMin} - {recipe.fairSellMax} coins"));
        SetOfferButtonText(SellOfferType.Risky, FormatOfferText("RISKY", "55%", $"{recipe.riskySellMin} - {recipe.riskySellMax} coins"));
        SetOfferButtonText(SellOfferType.TemptFate, FormatOfferText("TEMPT FATE", "20%", $"{recipe.temptFateReward} coins"));
    }

    private void SetOfferButtonLayout(SellOfferType type, float minX, float minY, float maxX, float maxY)
    {
        RectTransform rect = sellOfferButtons[type].GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetOfferButtonText(SellOfferType type, string text)
    {
        sellOfferButtons[type].GetComponentInChildren<TMP_Text>().text = text;
    }

    private static string FormatOfferText(string offerName, string chance, string reward)
    {
        return $"<b><size=112%>{offerName}</size></b>\n<size=96%>{chance}</size>\n<nobr><size=88%>{reward}</size></nobr>";
    }

    private void SetLegacySellControlsVisible(bool visible)
    {
        if (priceSlider != null) priceSlider.gameObject.SetActive(visible);
        if (priceValueText != null) priceValueText.gameObject.SetActive(visible);
        if (confirmSellButton != null) confirmSellButton.gameObject.SetActive(visible);
    }

    private void ResolveSellOffer(SellOfferType offerType)
    {
        if (pendingSellItem == null)
            return;

        InventoryItem item = pendingSellItem;
        bool isJunk = item.category == ItemCategory.Junk || item.itemName == junkItemName;
        float successChance;
        int reward;

        if (isJunk)
        {
            if (offerType != SellOfferType.Safe && offerType != SellOfferType.Risky)
                return;

            successChance = offerType == SellOfferType.Safe ? 1f : junkRiskyChance;
            reward = offerType == SellOfferType.Safe
                ? junkSafeReward
                : Random.Range(junkRiskyMinReward, junkRiskyMaxReward + 1);
        }
        else
        {
            if (!TryGetRecipe(item.itemName, out Recipe recipe))
                return;

            successChance = GetProductOfferChance(offerType);
            switch (offerType)
            {
                case SellOfferType.Safe:
                    reward = Random.Range(recipe.safeSellMin, recipe.safeSellMax + 1);
                    break;
                case SellOfferType.Fair:
                    reward = Random.Range(recipe.fairSellMin, recipe.fairSellMax + 1);
                    break;
                case SellOfferType.Risky:
                    reward = Random.Range(recipe.riskySellMin, recipe.riskySellMax + 1);
                    break;
                default:
                    reward = recipe.temptFateReward;
                    break;
            }
        }

        pendingSellItem = null;
        sellConfirmPanel.SetActive(false);

        if (successChance >= 1f || Random.value < successChance)
        {
            SellItem(item, reward);
            return;
        }

        lockedItemsToday.Add(item.itemName);
        RefreshSellUI();
    }

    private float GetProductOfferChance(SellOfferType type)
    {
        switch (type)
        {
            case SellOfferType.Safe: return safeOfferChance;
            case SellOfferType.Fair: return fairOfferChance;
            case SellOfferType.Risky: return riskyOfferChance;
            default: return temptFateOfferChance;
        }
    }

    private bool TryGetRecipe(string itemName, out Recipe recipe)
    {
        recipe = recipes.Find(candidate => candidate != null && candidate.potionName == itemName);
        return recipe != null;
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
        RefreshInventoryDependentUI();
    }

    void RemoveFromInventory(string name)
    {
        InventoryItem existing = inventory.Find(i => i.itemName == name);
        if (existing == null) return;

        existing.count--;
        if (existing.count <= 0) inventory.Remove(existing);

        RefreshInventoryDependentUI();
    }

    private void RefreshInventoryDependentUI()
    {
        if (sellPanel != null && sellPanel.activeInHierarchy && sellItemsParent != null)
            RefreshSellUI();

        if (inventoryPanel != null && inventoryPanel.activeInHierarchy)
            PopulateInventoryPanel();

        SellPanelRightUIBinder.RefreshVisible();
        FamilyMarketUI.RefreshIfVisible();
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
        {
            coins += newDayCurrencyReward;
            UpdateCoinsUI();
            StartMarketPhase();
        }

        yield return FadeDayTransitionOutRoutine();

        if (reachedGameOverDay)
        {
            yield return ShowDay20OutcomeCutsceneRoutine();
            isEndingDay = false;
            yield break;
        }

        DestroyDayTransitionCanvas();
        isEndingDay = false;
    }

    [ContextMenu("Build / Refresh Day 20 Cutscenes")]
    public void BuildDay20CutscenesEditablePreview()
    {
        EnsureDay20CutsceneObjects(false);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private IEnumerator ShowDay20OutcomeCutsceneRoutine(bool? forcedPlayerWon = null)
    {
        bool playerWon = forcedPlayerWon ?? HasResurrectionPotionInInventory();
        EnsureDay20CutsceneObjects(true);

        GameObject selectedRoot = playerWon ? day20WinCutsceneRoot : day20LoseCutsceneRoot;
        GameObject otherRoot = playerWon ? day20LoseCutsceneRoot : day20WinCutsceneRoot;

        if (day20CutsceneCanvasRoot != null)
            day20CutsceneCanvasRoot.SetActive(true);

        SetCutsceneVisible(otherRoot, false, 0f);

        CanvasGroup selectedGroup = SetCutsceneVisible(selectedRoot, true, 0f);
        AudioManager.Instance?.FadeOutMusic(day20CutsceneFadeDuration);
        PlayDay20Ambience(playerWon);

        if (selectedGroup != null)
        {
            if (playerWon)
            {
                yield return PlayDay20WinSequenceRoutine(selectedGroup);
            }
            else
            {
                yield return PlayDay20LoseZoomRoutine(selectedRoot, selectedGroup);
            }

            yield return FadeCanvasGroup(selectedGroup, 1f, 0f, day20CutsceneFadeDuration);
        }
        else
        {
            yield return new WaitForSecondsRealtime(day20CutsceneHoldDuration);
        }

        SetCutsceneVisible(selectedRoot, false, 0f);

        if (day20CutsceneCanvasRoot != null)
            day20CutsceneCanvasRoot.SetActive(false);

        ShowGameOverScreen(playerWon);
    }

    private bool HasResurrectionPotionInInventory()
    {
        if (inventory == null || resurrectionPotionNames == null)
            return false;

        for (int i = 0; i < inventory.Count; i++)
        {
            InventoryItem item = inventory[i];
            if (item == null || item.count <= 0)
                continue;

            if (IsResurrectionPotionName(item.itemName))
                return true;
        }

        return false;
    }

    private void EnsureDay20CutsceneObjects(bool runtime)
    {
        Canvas canvas = EnsureDay20CutsceneCanvas(runtime);
        if (canvas == null)
            return;

        day20WinCutsceneRoot = EnsureDay20CutsceneRoot(
            canvas.transform,
            day20WinCutsceneRoot,
            "Day 20 Win Cutscene",
            null,
            "",
            "");

        day20WinPotionCloseupRoot = EnsureDay20PotionCloseupStepRoot(
            day20WinCutsceneRoot.transform,
            day20WinPotionCloseupRoot,
            "01 Finished Potion Closeup");

        day20WinGraveSpillRoot = EnsureDay20CutsceneStepRoot(
            day20WinCutsceneRoot.transform,
            day20WinGraveSpillRoot,
            "02 Potion Into Grave",
            "Cinematics/Vivien flamel 2");

        day20WinVivianShopRoot = EnsureDay20CutsceneStepRoot(
            day20WinCutsceneRoot.transform,
            day20WinVivianShopRoot,
            "03 Vivian In Shop Unknown",
            "Cinematics/Day20WinVivianShopUnknown",
            day20WinVivianShopBackgroundSprite);

        EnsureWinOutcomeNewspaper(day20WinVivianShopRoot != null ? day20WinVivianShopRoot.transform : null);

        day20LoseCutsceneRoot = EnsureDay20CutsceneRoot(
            canvas.transform,
            day20LoseCutsceneRoot,
            "Day 20 Lose Cutscene",
            "Cinematics/Vivian F 3",
            "",
            "");
    }

    private Canvas EnsureDay20CutsceneCanvas(bool runtime)
    {
        if (day20CutsceneCanvasRoot == null)
            day20CutsceneCanvasRoot = GameObject.Find("Day 20 Outcome Cutscenes Canvas");

        bool created = false;
        if (day20CutsceneCanvasRoot == null)
        {
            day20CutsceneCanvasRoot = new GameObject(
                "Day 20 Outcome Cutscenes Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            created = true;
        }

        Canvas canvas = day20CutsceneCanvasRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = day20CutsceneCanvasRoot.AddComponent<Canvas>();

        CanvasScaler scaler = day20CutsceneCanvasRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = day20CutsceneCanvasRoot.AddComponent<CanvasScaler>();

        if (day20CutsceneCanvasRoot.GetComponent<GraphicRaycaster>() == null)
            day20CutsceneCanvasRoot.AddComponent<GraphicRaycaster>();

        if (day20CutsceneCanvasRoot.GetComponent<CanvasGroup>() == null)
            day20CutsceneCanvasRoot.AddComponent<CanvasGroup>();

        if (created)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 31950;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            day20CutsceneCanvasRoot.SetActive(!runtime);
        }

        return canvas;
    }

    private GameObject EnsureDay20CutsceneRoot(
        Transform canvasRoot,
        GameObject assignedRoot,
        string rootName,
        string resourceTexturePath,
        string defaultTitle,
        string defaultBody)
    {
        GameObject root = assignedRoot;
        if (root == null)
        {
            Transform found = FindDeepChild(canvasRoot, rootName);
            if (found != null)
                root = found.gameObject;
        }

        bool created = false;
        if (root == null)
        {
            root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvasRoot, false);
            created = true;
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null)
            rect = root.AddComponent<RectTransform>();

        if (created)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (!string.IsNullOrWhiteSpace(resourceTexturePath))
                CreateCutsceneBackground(root.transform, resourceTexturePath);
            if (!string.IsNullOrWhiteSpace(defaultTitle))
                CreateCutsceneText(root.transform, "Cutscene Title", defaultTitle, new Vector2(0f, 345f), new Vector2(1180f, 150f), 86f);
            if (!string.IsNullOrWhiteSpace(defaultBody))
                CreateCutsceneText(root.transform, "Cutscene Caption", defaultBody, new Vector2(0f, -385f), new Vector2(1250f, 120f), 34f);
            root.SetActive(false);
        }

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.AddComponent<CanvasGroup>();

        return root;
    }

    private GameObject EnsureDay20CutsceneStepRoot(
        Transform parent,
        GameObject assignedRoot,
        string rootName,
        string resourceTexturePath,
        Sprite backgroundSprite = null)
    {
        if (parent == null)
            return assignedRoot;

        GameObject root = assignedRoot;
        if (root == null)
        {
            Transform found = FindDeepChild(parent, rootName);
            if (found != null)
                root = found.gameObject;
        }

        bool created = false;
        if (root == null)
        {
            root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            created = true;
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null)
            rect = root.AddComponent<RectTransform>();

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.AddComponent<CanvasGroup>();

        if (created)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            root.SetActive(false);
        }

        EnsureCutsceneBackground(root.transform, resourceTexturePath, backgroundSprite);
        return root;
    }

    private GameObject EnsureDay20PotionCloseupStepRoot(Transform parent, GameObject assignedRoot, string rootName)
    {
        GameObject root = EnsureDay20CutsceneStepRoot(parent, assignedRoot, rootName, null);
        if (root == null)
            return null;

        HideGeneratedPotionCloseupBackground(root);

        Transform existingBackdrop = FindDeepChild(root.transform, "Editable Potion Backdrop");
        Image backdrop = existingBackdrop != null ? existingBackdrop.GetComponent<Image>() : null;
        if (backdrop == null)
        {
            backdrop = CreateOverlayImage("Editable Potion Backdrop", root.transform, new Color(0.46f, 0.46f, 0.46f, 1f));
            backdrop.raycastTarget = false;
        }
        else
        {
            backdrop.color = new Color(0.46f, 0.46f, 0.46f, 1f);
        }

        Transform existingPotion = FindDeepChild(root.transform, "Editable Resurrection Potion PNG");
        if (existingPotion != null)
        {
            RawImage existingImage = existingPotion.GetComponent<RawImage>();
            if (existingImage != null)
                existingImage.texture = Resources.Load<Texture2D>("Cinematics/Day20WinPoisonPotion");

            RectTransform existingRect = existingPotion as RectTransform;
            if (existingRect != null && existingRect.sizeDelta == new Vector2(560f, 840f))
                existingRect.sizeDelta = new Vector2(920f, 920f);

            return root;
        }

        GameObject potionObject = new GameObject("Editable Resurrection Potion PNG", typeof(RectTransform));
        potionObject.transform.SetParent(root.transform, false);

        RectTransform rect = potionObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(920f, 920f);

        RawImage potionImage = potionObject.AddComponent<RawImage>();
        potionImage.texture = Resources.Load<Texture2D>("Cinematics/Day20WinPoisonPotion");
        potionImage.color = Color.white;
        potionImage.raycastTarget = false;

        return root;
    }

    private void HideGeneratedPotionCloseupBackground(GameObject root)
    {
        if (root == null)
            return;

        RawImage[] images = root.GetComponentsInChildren<RawImage>(true);
        for (int i = 0; i < images.Length; i++)
        {
            RawImage image = images[i];
            if (image != null && image.texture != null && image.texture.name == "Day20WinPotionCloseup")
                image.gameObject.SetActive(false);
        }
    }

    private void EnsureWinOutcomeNewspaper(Transform parent)
    {
        if (parent == null)
            return;

        Transform oldFloatingText = parent.Find("Unknown Identity Text");
        if (oldFloatingText != null)
            oldFloatingText.gameObject.SetActive(false);

        Transform oldGeneratedPaper = FindDeepChild(parent, "Editable Revival Newspaper");
        if (oldGeneratedPaper != null)
            oldGeneratedPaper.gameObject.SetActive(false);

        Transform existingPaper = FindDeepChild(parent, "Editable Win Newspaper");
        GameObject newspaperObject = existingPaper != null
            ? existingPaper.gameObject
            : new GameObject("Editable Win Newspaper", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));

        newspaperObject.transform.SetParent(parent, false);

        RectTransform paperRect = newspaperObject.GetComponent<RectTransform>();
        if (paperRect == null)
            paperRect = newspaperObject.AddComponent<RectTransform>();

        paperRect.anchorMin = new Vector2(0.5f, 0.5f);
        paperRect.anchorMax = new Vector2(0.5f, 0.5f);
        paperRect.pivot = new Vector2(0.5f, 0.5f);
        paperRect.anchoredPosition = day20WinVivianShopNewspaperPosition;
        paperRect.sizeDelta = day20WinVivianShopNewspaperSize;
        paperRect.localRotation = Quaternion.Euler(0f, 0f, day20WinVivianShopNewspaperRotation);

        CanvasGroup group = newspaperObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = newspaperObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;
        group.blocksRaycasts = false;

        Image paper = newspaperObject.GetComponent<Image>();
        if (paper == null)
            paper = newspaperObject.AddComponent<Image>();

        paper.sprite = day20WinVivianShopNewspaperSprite != null
            ? day20WinVivianShopNewspaperSprite
            : CreateCleanNewspaperPaperSprite(560, 690);
        paper.type = Image.Type.Simple;
        paper.preserveAspect = true;
        paper.raycastTarget = false;

        Shadow shadow = newspaperObject.GetComponent<Shadow>();
        if (shadow == null)
            shadow = newspaperObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
        shadow.effectDistance = new Vector2(12f, -12f);

        if (day20WinVivianShopNewspaperSprite != null)
            return;

        TMP_Text masthead = CreateCutsceneText(
            newspaperObject.transform,
            "Newspaper Masthead",
            "THE VOODOO GAZETTE",
            new Vector2(0f, 286f),
            new Vector2(480f, 54f),
            38f);
        masthead.color = new Color(0.13f, 0.08f, 0.035f, 1f);
        masthead.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;

        Image topRule = CreateNewspaperRule(newspaperObject.transform, "Top Rule", new Vector2(0f, 250f), new Vector2(472f, 4f));
        topRule.color = new Color(0.17f, 0.1f, 0.045f, 1f);

        TMP_Text issue = CreateCutsceneText(
            newspaperObject.transform,
            "Newspaper Issue Line",
            "No. 20        New Bordeaux        Price: 5 Cents",
            new Vector2(0f, 228f),
            new Vector2(460f, 28f),
            16f);
        issue.color = new Color(0.17f, 0.1f, 0.045f, 1f);
        issue.fontStyle = FontStyles.Normal;

        TMP_Text label = CreateCutsceneText(
            newspaperObject.transform,
            "Main Headline",
            day20WinUnknownIdentityText,
            new Vector2(0f, 128f),
            new Vector2(478f, 128f),
            42f);
        label.color = new Color(0.08f, 0.055f, 0.028f, 1f);
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;

        Image headlineRule = CreateNewspaperRule(newspaperObject.transform, "Headline Rule", new Vector2(0f, 45f), new Vector2(448f, 4f));
        headlineRule.color = new Color(0.17f, 0.1f, 0.045f, 1f);

        TMP_Text flavor = CreateCutsceneText(
            newspaperObject.transform,
            "Flavor Text",
            "POTION WORKS!\nWE CAN REVIVE!\n\nWitnesses report impossible movement after the forbidden brew touched the grave soil.",
            new Vector2(0f, -104f),
            new Vector2(440f, 210f),
            25f);
        flavor.color = new Color(0.14f, 0.08f, 0.035f, 1f);
        flavor.fontStyle = FontStyles.Bold;
        flavor.alignment = TextAlignmentOptions.Center;
    }

    private void CreateCutsceneBackground(Transform parent, string resourceTexturePath)
    {
        GameObject imageObject = new GameObject("Editable Background Image", typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = imageObject.AddComponent<RawImage>();
        image.texture = Resources.Load<Texture2D>(resourceTexturePath);
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void EnsureCutsceneBackground(Transform parent, string resourceTexturePath, Sprite backgroundSprite)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find("Editable Background Image");
        GameObject imageObject = existing != null ? existing.gameObject : new GameObject("Editable Background Image", typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        imageObject.transform.SetAsFirstSibling();

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = imageObject.AddComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (backgroundSprite != null)
        {
            RawImage rawImage = imageObject.GetComponent<RawImage>();
            if (rawImage != null)
            {
                if (Application.isPlaying)
                    Destroy(rawImage);
                else
                    DestroyImmediate(rawImage);
            }

            Image image = imageObject.GetComponent<Image>();
            if (image == null)
                image = imageObject.AddComponent<Image>();

            image.sprite = backgroundSprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return;
        }

        Image oldImage = imageObject.GetComponent<Image>();
        if (oldImage != null)
        {
            if (Application.isPlaying)
                Destroy(oldImage);
            else
                DestroyImmediate(oldImage);
        }

        RawImage raw = imageObject.GetComponent<RawImage>();
        if (raw == null)
            raw = imageObject.AddComponent<RawImage>();

        raw.texture = !string.IsNullOrEmpty(resourceTexturePath)
            ? Resources.Load<Texture2D>(resourceTexturePath)
            : null;
        raw.color = Color.white;
        raw.raycastTarget = false;
    }

    private Image CreateNewspaperRule(Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        Image rule = CreateOverlayImage(objectName, parent, new Color(0.17f, 0.1f, 0.045f, 1f));
        RectTransform rect = rule.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rule;
    }

    private static Sprite CreateCleanNewspaperPaperSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated Clean Revival Newspaper";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color baseColor = new Color32(198, 174, 132, 255);
        Color stainColor = new Color32(121, 88, 50, 255);
        Color edgeColor = new Color32(82, 60, 38, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);
                float edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                float grain = Mathf.PerlinNoise(x * 0.036f, y * 0.036f);
                float fibers = Mathf.PerlinNoise(x * 0.12f, y * 0.018f);
                float stain = Mathf.PerlinNoise(x * 0.011f + 17f, y * 0.012f + 31f);
                float vignette = Mathf.Clamp01(1f - edgeDistance / 82f);

                Color color = baseColor;
                color *= Mathf.Lerp(0.91f, 1.07f, grain);
                color = Color.Lerp(color, new Color32(230, 208, 164, 255), fibers * 0.14f);
                color = Color.Lerp(color, stainColor, Mathf.Clamp01((stain - 0.58f) * 1.55f) * 0.22f);
                color = Color.Lerp(color, edgeColor, vignette * 0.36f);

                float crease = Mathf.Clamp01(1f - Mathf.Abs(nx - 0.5f) / 0.014f) * 0.055f;
                color = Color.Lerp(color, edgeColor, crease);
                color.a = 1f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private TMP_Text CreateCutsceneText(Transform parent, string objectName, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.color = Color.white;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = fontSize;
        label.raycastTarget = false;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return label;
    }

    private CanvasGroup SetCutsceneVisible(GameObject root, bool visible, float alpha)
    {
        if (root == null)
            return null;

        root.SetActive(visible);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.AddComponent<CanvasGroup>();

        group.alpha = alpha;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        return group;
    }

    private void PlayDay20Ambience(bool playerWon)
    {
        AudioClip clip = playerWon ? day20WinAmbience : day20LoseAmbience;
        if (clip == null)
            return;

        if (day20CutsceneAudioSource == null)
            day20CutsceneAudioSource = gameObject.AddComponent<AudioSource>();

        day20CutsceneAudioSource.clip = clip;
        day20CutsceneAudioSource.loop = true;
        day20CutsceneAudioSource.volume = day20AmbienceVolume;
        day20CutsceneAudioSource.Play();
    }

    private void StopDay20Ambience()
    {
        if (day20CutsceneAudioSource == null)
            return;

        day20CutsceneAudioSource.Stop();
        day20CutsceneAudioSource.clip = null;
    }

    private IEnumerator PlayDay20WinSequenceRoutine(CanvasGroup rootGroup)
    {
        if (rootGroup != null)
            rootGroup.alpha = 1f;

        HideLegacyDirectWinRootVisuals();

        GameObject[] steps =
        {
            day20WinPotionCloseupRoot,
            day20WinGraveSpillRoot,
            day20WinVivianShopRoot
        };
        float[] holds =
        {
            day20WinPotionCloseupHoldDuration,
            day20WinGraveSpillHoldDuration,
            day20WinVivianShopHoldDuration
        };

        CanvasGroup previousGroup = null;
        GameObject previousStep = null;

        for (int i = 0; i < steps.Length; i++)
        {
            GameObject step = steps[i];
            CanvasGroup stepGroup = SetCutsceneVisible(step, true, 0f);
            if (stepGroup == null)
                continue;

            if (previousGroup == null)
            {
                yield return FadeCanvasGroup(stepGroup, 0f, 1f, day20WinStepFadeDuration);
            }
            else
            {
                yield return CrossfadeCanvasGroups(
                    previousGroup,
                    stepGroup,
                    day20WinStepFadeDuration);
                SetCutsceneVisible(previousStep, false, 0f);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, holds[i]));
            previousGroup = stepGroup;
            previousStep = step;
        }
    }

    private IEnumerator CrossfadeCanvasGroups(CanvasGroup outgoing, CanvasGroup incoming, float duration)
    {
        if (outgoing == null || incoming == null || duration <= 0f)
        {
            if (outgoing != null)
                outgoing.alpha = 0f;
            if (incoming != null)
                incoming.alpha = 1f;
            yield break;
        }

        incoming.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            outgoing.alpha = 1f - smoothProgress;
            incoming.alpha = smoothProgress;
            yield return null;
        }

        outgoing.alpha = 0f;
        incoming.alpha = 1f;
    }

    private void HideLegacyDirectWinRootVisuals()
    {
        if (day20WinCutsceneRoot == null)
            return;

        for (int i = 0; i < day20WinCutsceneRoot.transform.childCount; i++)
        {
            Transform child = day20WinCutsceneRoot.transform.GetChild(i);
            if (child == null ||
                child == day20WinPotionCloseupRoot?.transform ||
                child == day20WinGraveSpillRoot?.transform ||
                child == day20WinVivianShopRoot?.transform)
            {
                continue;
            }

            if (child.GetComponent<RawImage>() != null || child.GetComponent<TMP_Text>() != null)
                child.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayDay20LoseZoomRoutine(GameObject root, CanvasGroup group)
    {
        RawImage background = FindCutsceneBackground(root);
        if (background == null)
        {
            yield return FadeCanvasGroup(group, 0f, 1f, day20CutsceneFadeDuration);
            yield return new WaitForSecondsRealtime(day20CutsceneHoldDuration);
            yield break;
        }

        Texture2D cemeteryTexture = Resources.Load<Texture2D>("Cinematics/Vivian F 3");
        if (cemeteryTexture != null)
            background.texture = cemeteryTexture;

        background.color = day20LoseTint;
        background.uvRect = day20LoseStartView;

        yield return FadeCanvasGroup(group, 0f, 1f, day20CutsceneFadeDuration);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, day20LoseZoomOutDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = SmoothStep01(elapsed / duration);
            background.uvRect = LerpRect(day20LoseStartView, day20LoseEndView, progress);
            yield return null;
        }

        background.uvRect = day20LoseEndView;

        float remainingHold = Mathf.Max(0f, day20CutsceneHoldDuration - duration);
        if (remainingHold > 0f)
            yield return new WaitForSecondsRealtime(remainingHold);
    }

    private RawImage FindCutsceneBackground(GameObject root)
    {
        if (root == null)
            return null;

        RawImage[] images = root.GetComponentsInChildren<RawImage>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == "Editable Background Image")
                return images[i];
        }

        return images.Length > 0 ? images[0] : null;
    }

    private static Rect LerpRect(Rect from, Rect to, float progress)
    {
        return new Rect(
            Mathf.Lerp(from.x, to.x, progress),
            Mathf.Lerp(from.y, to.y, progress),
            Mathf.Lerp(from.width, to.width, progress),
            Mathf.Lerp(from.height, to.height, progress));
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
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

    private void ShowGameOverScreen(bool playerWon = false)
    {
        Canvas canvas = EnsureDayTransitionCanvas();
        Transform root = canvas.transform;
        ClearChildren(root);

        Image background = CreateOverlayImage("Game Over Background", root, Color.black);
        background.raycastTarget = true;

        string resultText = playerWon ? gameWonText : (!string.IsNullOrWhiteSpace(gameLostText) ? gameLostText : gameOverText);
        TMP_Text title = CreateOverlayText("Game Over Text", root, resultText);
        title.color = playerWon ? gameWonTextColor : gameLostTextColor;
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
        StopDay20Ambience();
        currentDay = 1;
        isEndingDay = false;
        DestroyDayTransitionCanvas();
        AudioManager.Instance?.PlayGameplayMusicImmediately();
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

        GameObject obj = Instantiate(floatingCoinTextPrefab, floatingTextSpawnPoint, false);
        Vector2 offset = amount < 0 ? floatingCoinBuyOffset : floatingCoinSellOffset;

        Canvas floatingCanvas = obj.GetComponent<Canvas>();
        if (floatingCanvas == null)
            floatingCanvas = obj.AddComponent<Canvas>();
        floatingCanvas.overrideSorting = true;
        floatingCanvas.sortingOrder = floatingCoinSortingOrder;

        RectTransform rect = obj.transform as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = offset;
            rect.sizeDelta = floatingCoinTextSize;
        }
        else
        {
            obj.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }

        FloatingCoinText floatText = obj.GetComponent<FloatingCoinText>();
        TMP_Text tmpText = obj.GetComponent<TMP_Text>();

        if (tmpText != null)
        {
            if (floatingCoinFont != null)
                tmpText.font = floatingCoinFont;
            tmpText.fontSize = floatingCoinFontSize;
        }

        if (floatText == null)
        {
            Debug.LogWarning("The floating coin text prefab needs a FloatingCoinText component.", obj);
            Destroy(obj);
            return;
        }

        floatText.floatSpeed = floatingCoinFloatSpeed;
        floatText.lifetime = floatingCoinLifetime;

        if (amount < 0)
            floatText.SetText(amount.ToString(), floatingCoinDeductedColor);
        else
            floatText.SetText("+" + amount.ToString(), floatingCoinAddedColor);
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

        if (Input.GetKeyDown(KeyCode.W))
            ActivateWinCheat();

        if (Input.GetKeyDown(KeyCode.L))
            ActivateLoseCheat();

        if (Input.GetKeyDown(KeyCode.F1))
            SkipDay20CutsceneCheat();

        if (Input.GetKeyDown(KeyCode.F2))
            FTUEManager.DisableAllTutorials();
    }

    private void SkipDay20CutsceneCheat()
    {
        if (day20CutsceneCanvasRoot == null || !day20CutsceneCanvasRoot.activeInHierarchy)
            return;

        bool playerWon = HasResurrectionPotionInInventory();

        StopAllCoroutines();
        SetCutsceneVisible(day20WinCutsceneRoot, false, 0f);
        SetCutsceneVisible(day20LoseCutsceneRoot, false, 0f);
        day20CutsceneCanvasRoot.SetActive(false);

        isEndingDay = false;
        ShowGameOverScreen(playerWon);
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
        coins += cheatCoinsAmount;
        UpdateCoinsUI();
        ShowFloatingCoins(cheatCoinsAmount);
    }

    private void ActivateWinCheat()
    {
        Recipe resurrectionRecipe = FindResurrectionRecipe();
        if (resurrectionRecipe == null)
        {
            Debug.LogWarning("Win cheat could not find a resurrection recipe.");
            return;
        }

        PrepareDay19MergeCheatState();
        inventory.Clear();
        selectedCraftingItems.Clear();

        foreach (string ingredientName in resurrectionRecipe.ingredients)
            AddExactInventoryItemForCheat(ingredientName);

        OpenCrafting();
        RefreshInventoryDependentUI();
    }

    private void ActivateLoseCheat()
    {
        PrepareDay19MergeCheatState();
        inventory.Clear();
        selectedCraftingItems.Clear();

        string junkIngredient = FindLoseCheatIngredient();
        if (!string.IsNullOrWhiteSpace(junkIngredient))
        {
            for (int i = 0; i < 3; i++)
                AddExactInventoryItemForCheat(junkIngredient);
        }
        else
        {
            Debug.LogWarning("Lose cheat could not find an ingredient that is safe to merge into Junk.");
        }

        OpenCrafting();
        RefreshInventoryDependentUI();
    }

    private string FindLoseCheatIngredient()
    {
        if (markets == null)
            return null;

        foreach (Market market in markets)
        {
            if (market == null || market.items == null)
                continue;

            foreach (MarketItem item in market.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemName) ||
                    item.category == ItemCategory.Potion || item.category == ItemCategory.Junk)
                {
                    continue;
                }

                bool createsRecipe = false;
                if (recipes != null)
                {
                    foreach (Recipe recipe in recipes)
                    {
                        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count != 3)
                            continue;

                        createsRecipe = true;
                        foreach (string ingredient in recipe.ingredients)
                        {
                            if (NormalizeName(ingredient) != NormalizeName(item.itemName))
                            {
                                createsRecipe = false;
                                break;
                            }
                        }

                        if (createsRecipe)
                            break;
                    }
                }

                if (!createsRecipe)
                    return item.itemName;
            }
        }

        return null;
    }

    private void PrepareDay19MergeCheatState()
    {
        currentDay = Mathf.Max(1, gameOverDay - 1);
        isEndingDay = false;
        lockedItemsToday.Clear();
        ReturnCraftingItemsToInventory();

        if (sellConfirmPanel != null)
            sellConfirmPanel.SetActive(false);
        pendingSellItem = null;
        DestroyDayTransitionCanvas();
    }

    private IEnumerator ShowImmediateWinCutsceneAfterMergeRoutine()
    {
        isEndingDay = true;
        craftingExitRequired = false;
        pendingSellItem = null;

        if (marketPanel != null) marketPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (sellPanel != null) sellPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (sellConfirmPanel != null) sellConfirmPanel.SetActive(false);

        yield return null;
        yield return ShowDay20OutcomeCutsceneRoutine(true);
        isEndingDay = false;
    }

    private Recipe FindResurrectionRecipe()
    {
        if (recipes == null)
            return null;

        Recipe fallback = null;
        for (int i = 0; i < recipes.Count; i++)
        {
            Recipe recipe = recipes[i];
            if (recipe == null)
                continue;

            if (IsResurrectionPotionName(recipe.potionName))
                return recipe;

            if (fallback == null && NormalizeName(recipe.potionName) == NormalizeName(cheatWinPotionName))
                fallback = recipe;
        }

        return fallback;
    }

    private bool IsResurrectionPotionName(string itemName)
    {
        string normalized = NormalizeName(itemName);
        if (normalized == NormalizeName(cheatWinPotionName))
            return true;

        if (resurrectionPotionNames == null)
            return false;

        for (int i = 0; i < resurrectionPotionNames.Length; i++)
        {
            if (normalized == NormalizeName(resurrectionPotionNames[i]))
                return true;
        }

        return false;
    }

    private void AddExactInventoryItemForCheat(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return;

        ItemCategory category = ItemCategory.Herbs;
        string description = "";
        Sprite icon = GetIconByNameInsensitive(itemName);

        if (TryGetMarketItemByName(itemName, out MarketItem marketItem))
        {
            category = marketItem.category;
            description = marketItem.description;
            if (icon == null)
                icon = marketItem.icon;
        }
        else if (TryGetRecipeByName(itemName, out Recipe recipe))
        {
            category = recipe.category;
            if (icon == null)
                icon = recipe.icon;
        }

        inventory.Add(new InventoryItem
        {
            itemName = itemName,
            count = 1,
            category = category,
            description = description,
            icon = icon
        });
    }

    private bool TryGetMarketItemByName(string itemName, out MarketItem foundItem)
    {
        if (markets != null)
        {
            foreach (Market market in markets)
            {
                if (market == null || market.items == null)
                    continue;

                foreach (MarketItem item in market.items)
                {
                    if (item != null && NormalizeName(item.itemName) == NormalizeName(itemName))
                    {
                        foundItem = item;
                        return true;
                    }
                }
            }
        }

        foundItem = null;
        return false;
    }

    private bool TryGetRecipeByName(string itemName, out Recipe foundRecipe)
    {
        if (recipes != null)
        {
            foreach (Recipe recipe in recipes)
            {
                if (recipe != null && NormalizeName(recipe.potionName) == NormalizeName(itemName))
                {
                    foundRecipe = recipe;
                    return true;
                }
            }
        }

        foundRecipe = null;
        return false;
    }

    [ContextMenu("Build / Refresh Cheat Menu")]
    public void BuildCheatMenuEditablePreview()
    {
        EnsureCheatMenuObjects(false);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void OpenCheatMenu()
    {
        EnsureCheatMenuObjects(true);
        if (cheatMenuCanvasRoot != null)
            cheatMenuCanvasRoot.SetActive(true);
        if (cheatMenuRoot != null)
            cheatMenuRoot.SetActive(true);
    }

    private void CloseCheatMenu()
    {
        if (cheatMenuRoot != null)
            cheatMenuRoot.SetActive(false);
        if (cheatMenuCanvasRoot != null)
            cheatMenuCanvasRoot.SetActive(false);
    }

    private void CheatAddCoins()
    {
        coins += cheatCoinsAmount;
        UpdateCoinsUI();
        ShowFloatingCoins(cheatCoinsAmount);
        CloseCheatMenu();
    }

    private void CheatLetMeWin()
    {
        SetDay19SellPanelState();
        AddOrRefreshCheatWinPotion();
        CloseCheatMenu();
    }

    private void CheatIWantToLose()
    {
        SetDay19SellPanelState();
        RemoveResurrectionPotionsFromInventory();
        CloseCheatMenu();
    }

    private void SetDay19SellPanelState()
    {
        currentDay = Mathf.Max(1, gameOverDay - 1);
        isEndingDay = false;
        lockedItemsToday.Clear();
        ReturnCraftingItemsToInventory();
        PopulateInventoryPanel();
        OpenSell();
    }

    private void AddOrRefreshCheatWinPotion()
    {
        string potionName = string.IsNullOrWhiteSpace(cheatWinPotionName) ? "Ultimate Potion" : cheatWinPotionName;
        Sprite icon = GetIconByNameInsensitive(potionName);
        InventoryItem existing = inventory.Find(i => NormalizeName(i.itemName) == NormalizeName(potionName));
        if (existing != null)
        {
            existing.count = Mathf.Max(1, existing.count);
            existing.category = ItemCategory.Potion;
            if (existing.icon == null)
                existing.icon = icon;
            if (string.IsNullOrWhiteSpace(existing.description))
                existing.description = cheatWinPotionDescription;
        }
        else
        {
            inventory.Add(new InventoryItem
            {
                itemName = potionName,
                count = 1,
                category = ItemCategory.Potion,
                description = cheatWinPotionDescription,
                icon = icon
            });
        }

        PopulateInventoryPanel();
        RefreshSellUI();
        SellPanelRightUIBinder.RefreshVisible();
    }

    private void RemoveResurrectionPotionsFromInventory()
    {
        inventory.RemoveAll(IsResurrectionPotionInventoryItem);
        PopulateInventoryPanel();
        RefreshSellUI();
        SellPanelRightUIBinder.RefreshVisible();
    }

    private bool IsResurrectionPotionInventoryItem(InventoryItem item)
    {
        if (item == null || resurrectionPotionNames == null)
            return false;

        string itemName = NormalizeName(item.itemName);
        for (int i = 0; i < resurrectionPotionNames.Length; i++)
        {
            if (itemName == NormalizeName(resurrectionPotionNames[i]))
                return true;
        }

        return itemName == NormalizeName(cheatWinPotionName);
    }

    private void EnsureCheatMenuObjects(bool runtime)
    {
        Canvas canvas = EnsureCheatMenuCanvas(runtime);
        if (canvas == null)
            return;

        if (cheatMenuRoot == null)
        {
            Transform foundRoot = FindDeepChild(canvas.transform, "Cheat Menu");
            if (foundRoot != null)
                cheatMenuRoot = foundRoot.gameObject;
        }

        bool createdRoot = false;
        if (cheatMenuRoot == null)
        {
            cheatMenuRoot = new GameObject("Cheat Menu", typeof(RectTransform), typeof(CanvasGroup));
            cheatMenuRoot.transform.SetParent(canvas.transform, false);
            createdRoot = true;
        }

        RectTransform rootRect = cheatMenuRoot.GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = cheatMenuRoot.AddComponent<RectTransform>();

        CanvasGroup group = cheatMenuRoot.GetComponent<CanvasGroup>();
        if (group == null)
            group = cheatMenuRoot.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        if (createdRoot)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image backdrop = CreateOverlayImage("Cheat Menu Backdrop", cheatMenuRoot.transform, new Color(0.02f, 0.01f, 0.025f, 0.78f));
            StretchRect(backdrop.rectTransform);

            CreateEditableText(cheatMenuRoot.transform, "Cheat Menu Title", "CHEAT MENU", new Vector2(0f, 180f), new Vector2(620f, 110f), 64f);
        }

        cheatAddCoinsButton = EnsureCheatButton(
            cheatMenuRoot.transform,
            cheatAddCoinsButton,
            "Cheat Add Coins Button",
            cheatAddCoinsButtonText,
            new Vector2(0f, 48f));

        cheatLetMeWinButton = EnsureCheatButton(
            cheatMenuRoot.transform,
            cheatLetMeWinButton,
            "Cheat Let Me Win Button",
            cheatLetMeWinButtonText,
            new Vector2(0f, -72f));

        cheatIWantToLoseButton = EnsureCheatButton(
            cheatMenuRoot.transform,
            cheatIWantToLoseButton,
            "Cheat I Want To Lose Button",
            cheatIWantToLoseButtonText,
            new Vector2(0f, -192f));

        WireCheatButton(cheatAddCoinsButton, CheatAddCoins);
        WireCheatButton(cheatLetMeWinButton, CheatLetMeWin);
        WireCheatButton(cheatIWantToLoseButton, CheatIWantToLose);

        if (createdRoot)
            cheatMenuRoot.SetActive(false);
    }

    private Canvas EnsureCheatMenuCanvas(bool runtime)
    {
        if (cheatMenuCanvasRoot == null)
            cheatMenuCanvasRoot = GameObject.Find("Cheat Menu Canvas");

        bool created = false;
        if (cheatMenuCanvasRoot == null)
        {
            cheatMenuCanvasRoot = new GameObject(
                "Cheat Menu Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            created = true;
        }

        Canvas canvas = cheatMenuCanvasRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = cheatMenuCanvasRoot.AddComponent<Canvas>();

        CanvasScaler scaler = cheatMenuCanvasRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = cheatMenuCanvasRoot.AddComponent<CanvasScaler>();

        if (cheatMenuCanvasRoot.GetComponent<GraphicRaycaster>() == null)
            cheatMenuCanvasRoot.AddComponent<GraphicRaycaster>();

        if (created)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = CheatMenuSortingOrder;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            cheatMenuCanvasRoot.SetActive(!runtime);
        }

        return canvas;
    }

    private Button EnsureCheatButton(Transform parent, Button assignedButton, string objectName, string defaultText, Vector2 position)
    {
        Button button = assignedButton;
        if (button == null)
        {
            Transform found = FindDeepChild(parent, objectName);
            if (found != null)
                button = found.GetComponent<Button>();
        }

        bool created = false;
        if (button == null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.05f, 0.12f, 0.94f);
            button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            created = true;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
            rect = button.gameObject.AddComponent<RectTransform>();

        if (created)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 82f);

            TMP_Text label = CreateEditableText(button.transform, "Label", defaultText, Vector2.zero, rect.sizeDelta, 34f);
            label.color = new Color(1f, 0.86f, 0.62f, 1f);
        }

        return button;
    }

    private void WireCheatButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private TMP_Text CreateEditableText(Transform parent, string objectName, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = fontSize;
        label.raycastTarget = false;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return label;
    }

    private void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
