using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Sell Price Limits")]
    public int minSellPrice;
    public int maxSellPrice;
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


    private InventoryItem pendingSellItem;
    private List<InventoryItem> inventory = new List<InventoryItem>();
    private List<InventoryItem> selectedCraftingItems = new List<InventoryItem>();
    public List<InventoryItem> GetInventoryItems()
    {
        return inventory;
    }
    public ObjectiveManager objectiveManager;

    // ------------------- START -------------------
    void Start()
    {
        StartMarketPhase();
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
        marketPanel.SetActive(false);
        itemsPanel.SetActive(true);
        ad.PlaySfx(vol, SFX.EnteredShop, pitch);
        ClearChildren(itemsButtonsParent);

        foreach (MarketItem item in market.items)
        {
            GameObject btn = Instantiate(buttonPrefab, itemsButtonsParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            txt.text = $"{item.itemName} - {item.price} coins";

            ApplyCategoryStyle(btn, item.category);

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                BuyItem(item);
            });
        }
    }

    void BuyItem(MarketItem item)
    {
        if (coins < item.price) return;

        coins -= item.price;
        AddToInventory(item.itemName, item.category);
        ShowInventoryStar();
        PopulateInventoryPanel();
        UpdateCoinsUI();
        ShowFloatingCoins(-item.price);
        ad.PlaySfx(vol, SFX.Buying, pitch);
        if (inventoryPanel.activeSelf)
            PopulateInventoryPanel();
        // Check if any tasks are now completed
        objectiveManager.UpdateTasksFromInventory(inventory);

    }

    // ------------------- CRAFTING -------------------
    public void OpenCrafting()
    {
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(true);
        sellPanel.SetActive(false);

        selectedCraftingItems.Clear();
        RefreshSelectedItemsUI();
        RefreshCraftingUI();
    }

    void RefreshCraftingUI()
    {
        ClearChildren(craftingItemsParent);

        foreach (InventoryItem item in inventory)
        {
            GameObject btn = Instantiate(buttonPrefab, craftingItemsParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            txt.text = $"{item.itemName} x{item.count}";

            ApplyCategoryStyle(btn, item.category);
            btn.GetComponent<Button>().onClick.AddListener(() => SelectCraftingItem(item));
        }
    }

    void SelectCraftingItem(InventoryItem item)
    {
        if (selectedCraftingItems.Count >= 3) return;
        if (item.count <= 0) return;

        // Add ONE UNIT of this item to crafting
        selectedCraftingItems.Add(new InventoryItem
        {
            itemName = item.itemName,
            category = item.category,
            count = 1
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
            GameObject txtObj = Instantiate(selectedItemTextPrefab, selectedItemsParent);
            TMP_Text txt = txtObj.GetComponent<TMP_Text>();
            //txt.text = selectedCraftingItems[i].itemName;
            txt.text = "";
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            updateText tag = txtObj.GetComponent<updateText>();

            tag.updateTheText(selectedCraftingItems[i].itemName);

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
                AddToInventory(recipe.potionName, recipe.category);
                craftedSomething = true;
                break;
            }
        }

        if (!craftedSomething)
        {
            AddToInventory(junkItemName, ItemCategory.Junk);
            ad.PlaySfx(vol, SFX.JunkMerge, pitch);
        }

        selectedCraftingItems.Clear();

        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        PopulateInventoryPanel();
        FindObjectOfType<ObjectiveManager>().CompleteMission(MissionType.MergeItems);

        if (inventoryPanel.activeSelf)
            PopulateInventoryPanel();
    }

    void ReturnCraftingItemsToInventory()
    {
        foreach (var item in selectedCraftingItems)
        {
            AddToInventory(item.itemName, item.category);
        }

        selectedCraftingItems.Clear();
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
        FindObjectOfType<ObjectiveManager>().CompleteMission(MissionType.SellItems);
        if (inventoryPanel.activeSelf)
            PopulateInventoryPanel();
    }

    void RefreshSellUI()
    {
        ClearChildren(sellItemsParent);

        foreach (InventoryItem item in inventory)
        {
            GameObject btn = Instantiate(buttonPrefab, sellItemsParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            txt.text = $"{item.itemName} x{item.count}";

            ApplyCategoryStyle(btn, item.category);
            btn.GetComponent<Button>().onClick.AddListener(() => OnSellClicked(item));
        }
    }
    void OnPriceSliderChanged(float value)
    {
        int price = Mathf.RoundToInt(value);
        priceValueText.text = price.ToString();
    }

    void OnSellClicked(InventoryItem item)
    {
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
    void AddToInventory(string name, ItemCategory category)
    {
        ShowObjectiveDiscoveryStar();
        InventoryItem existing = inventory.Find(i => i.itemName == name);
        if (existing != null) existing.count++;
        else inventory.Add(new InventoryItem { itemName = name, count = 1, category = category });
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
    }

    public void PopulateInventoryPanel()
    {
        foreach (Transform child in inventoryListParent)
            Destroy(child.gameObject);

        foreach (InventoryItem item in inventory)
        {
            GameObject row = Instantiate(inventoryRowPrefab, inventoryListParent);

            // Assign texts properly
            TMP_Text itemNameText = row.transform.Find("ItemNameText").GetComponent<TMP_Text>();
            TMP_Text itemCountText = row.transform.Find("ItemCountText").GetComponent<TMP_Text>();
            Button investigateBtn = row.transform.Find("InvestigateButton").GetComponent<Button>();

            itemNameText.text = item.itemName;
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
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);
        inventoryPanel.SetActive(false);

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

}
