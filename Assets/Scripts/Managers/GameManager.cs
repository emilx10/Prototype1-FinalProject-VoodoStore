using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
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

    private HashSet<string> discoveredRecipes = new HashSet<string>();

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
    [Header("Known Recipes UI")]
    [SerializeField] private GameObject knownRecipesPanel;
    [SerializeField] private Transform knownRecipesParent;
    [SerializeField] private GameObject knownRecipePrefab;

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


    private HashSet<string> discoveredItems = new HashSet<string>();
    private bool hasUnseenNewItem = false;
    private HashSet<string> lockedItemsToday = new HashSet<string>();

    [Header("Inventory Button")]
    [SerializeField] private Button inventoryButton;
    public ButtonBreather inventoryBreather;

    public List<InventoryItem> GetInventoryItems()
    {
        return inventory;
    }
    public ObjectiveManager objectiveManager;

    public void OpenKnownRecipes()
    {
        knownRecipesPanel.SetActive(true);
        PopulateKnownRecipesUI();
    }

    public void CloseKnownRecipes()
    {
        knownRecipesPanel.SetActive(false);
        PopulateKnownRecipesUI();
    }

    void PopulateKnownRecipesUI()
    {
        ClearChildren(knownRecipesParent);

        foreach (Recipe recipe in recipes)
        {
            if (!discoveredRecipes.Contains(recipe.potionName))
                continue;

            GameObject obj = Instantiate(knownRecipePrefab, knownRecipesParent);

            // RESULT ICON
            Transform resultIconTransform = obj.transform.Find("ResultIcon");
            if (resultIconTransform != null)
            {
                Image resultIcon = resultIconTransform.GetComponent<Image>();
                resultIcon.sprite = recipe.icon;
                resultIcon.enabled = recipe.icon != null;
            }

            // INGREDIENTS ROW
            Transform ingredientsRow = obj.transform.Find("IngredientsRow");

            if (ingredientsRow != null)
            {
                // Remove any LayoutGroup to control positions manually
                LayoutGroup lg = ingredientsRow.GetComponent<LayoutGroup>();
                if (lg != null) Destroy(lg);

                float spacing = 60f; // X spacing between icons
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    string ingredientName = recipe.ingredients[i];
                    Sprite ingredientIcon = GetIconByNameInsensitive(ingredientName);

                    GameObject iconObj = new GameObject("IngredientIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconObj.transform.SetParent(ingredientsRow, false);

                    Image img = iconObj.GetComponent<Image>();
                    img.sprite = ingredientIcon;
                    img.enabled = ingredientIcon != null;

                    // Set size manually
                    RectTransform rt = iconObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(50, 50); // adjust as needed
                    rt.anchoredPosition = new Vector2(i * spacing, 0); // horizontal placement
                }
            }
        }
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
        EnsureFrontCanvas(itemsPanel, ShopItemsSortingOrder);
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

    // ------------------- MARKET -------------------
    public void StartMarketPhase()
    {
        marketPanel.SetActive(true);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);
    }

    public void OpenMarket(Market market)
    {
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
        ad.PlaySfx(vol, SFX.Buying, pitch);

        OnItemBought?.Invoke(item.icon);
        objectiveManager.UpdateTasksFromInventory(GetInventoryItems());

        RefreshMarketItemsUI(); // <-- just refresh buttons and counts
    }

    void RefreshMarketItemsUI()
    {
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
        ClearChildren(selectedItemsParent);

        for (int i = 0; i < selectedCraftingItems.Count; i++)
        {
            GameObject btnObj = Instantiate(selectedItemTextPrefab, selectedItemsParent);

            // Get the Icon inside Button child
            Image iconImage = btnObj.transform.Find("Button/Icon")?.GetComponent<Image>();
            if (iconImage != null && selectedCraftingItems[i].icon != null)
            {
                iconImage.sprite = selectedCraftingItems[i].icon;
                iconImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"Icon missing for item {selectedCraftingItems[i].itemName}");
            }

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

    public void MergeItems()
    {
        if (selectedCraftingItems.Count < 2) return;
        StartCoroutine(MergeAnimationCoroutine());
    }

    private IEnumerator MergeAnimationCoroutine()
    {
        // Collect all images to animate
        List<Image> imagesToAnimate = new List<Image>();

        foreach (Transform buttonTransform in selectedItemsParent)
        {

            // Icon child Image
            Transform bTransform = buttonTransform.Find("Button");
            if (bTransform != null)
            {
                Image iconImg = bTransform.GetComponent<Image>();
                if (iconImg != null)
                {
                    Material iconMat = new Material(iconImg.material); // unique material
                    iconImg.material = iconMat;
                    iconMat.SetFloat("_LifeTime", -2f);
                    imagesToAnimate.Add(iconImg);
                }
            }
            Transform iconTransform = buttonTransform.Find("Button/Icon");
            if (iconTransform != null)
            {
                Image iconImg = iconTransform.GetComponent<Image>();
                if (iconImg != null)
                {
                    Material iconMat = new Material(iconImg.material); // unique material
                    iconImg.material = iconMat;
                    iconMat.SetFloat("_LifeTime", -2f);
                    imagesToAnimate.Add(iconImg);
                }
            }
        }

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float value = Mathf.Lerp(-2f, 1f, elapsed / duration);

            foreach (Image img in imagesToAnimate)
            {
                img.material.SetFloat("_LifeTime", value);
                img.SetMaterialDirty(); // force Canvas to redraw
                Debug.Log($"Animating {img.name} _LifeTime = {value}");
            }

            yield return null;
        }

        // Ensure final value
        foreach (Image img in imagesToAnimate)
        {
            img.material.SetFloat("_LifeTime", 1f);
            img.SetMaterialDirty();
            Debug.Log($"Final _LifeTime for {img.name} = 1");
        }

        // After animation finishes, give the item
        CraftSelectedItems();
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
                discoveredRecipes.Add(recipe.potionName);
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
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(true);
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
        RandomizeMarketStock();
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        lockedItemsToday.Clear();
        objectiveManager.ResetDailyInvestigations();
        PopulateInventoryPanel();

        StartMarketPhase();
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
