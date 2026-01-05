using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MarketItem
{
    public string itemName;
    public int price; // buy price ONLY

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
}

public class GameManager : MonoBehaviour
{
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
    [SerializeField] private TMP_InputField priceInputField;
    [SerializeField] private TMP_Text sellItemNameText;
    [SerializeField] private Button confirmSellButton;

    private InventoryItem pendingSellItem;


    private List<InventoryItem> inventory = new List<InventoryItem>();
    private List<InventoryItem> selectedCraftingItems = new List<InventoryItem>();

    void Start()
    {
        StartMarketPhase();
        UpdateInventoryUI();
        UpdateCoinsUI();
    }

    #region Market

    public void StartMarketPhase()
    {
        marketPanel.SetActive(true);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);

        ClearChildren(marketButtonsParent);

        foreach (Market market in markets)
        {
            GameObject btn = Instantiate(buttonPrefab, marketButtonsParent);
            btn.GetComponentInChildren<TMP_Text>().text = market.marketName;
            btn.GetComponent<Button>().onClick.AddListener(() => OpenMarket(market));
        }
    }

    void OpenMarket(Market market)
    {
        marketPanel.SetActive(false);
        itemsPanel.SetActive(true);

        ClearChildren(itemsButtonsParent);

        foreach (MarketItem item in market.items)
        {
            GameObject btn = Instantiate(buttonPrefab, itemsButtonsParent);
            btn.GetComponentInChildren<TMP_Text>().text =
                item.itemName + " - " + item.price + " coins";

            btn.GetComponent<Button>().onClick.AddListener(() => BuyItem(item));
        }
    }

    void BuyItem(MarketItem item)
    {
        if (coins < item.price) return;

        coins -= item.price;
        AddToInventory(item.itemName);

        UpdateInventoryUI();
        UpdateCoinsUI();
    }

    #endregion

    #region Crafting

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
            btn.GetComponentInChildren<TMP_Text>().text =
                item.itemName + " x" + item.count;

            btn.GetComponent<Button>().onClick.AddListener(() => SelectCraftingItem(item));
        }
    }

    void SelectCraftingItem(InventoryItem item)
    {
        if (selectedCraftingItems.Contains(item)) return;
        if (selectedCraftingItems.Count >= 3) return;

        selectedCraftingItems.Add(item);
        RefreshSelectedItemsUI();
    }

    void RefreshSelectedItemsUI()
    {
        ClearChildren(selectedItemsParent);

        for (int i = 0; i < selectedCraftingItems.Count; i++)
        {
            GameObject txtObj = Instantiate(selectedItemTextPrefab, selectedItemsParent);
            TMP_Text txt = txtObj.GetComponent<TMP_Text>();
            txt.text = selectedCraftingItems[i].itemName;

            RectTransform rt = txtObj.GetComponent<RectTransform>();

            if (selectedCraftingItems.Count == 1)
            {
                rt.anchoredPosition = Vector2.zero;
            }
            else if (selectedCraftingItems.Count == 2)
            {
                rt.anchoredPosition = (i == 0) ? triangleLeftTop : triangleRightTop;
            }
            else // 3 items
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
            if (recipe.ingredients.Count != selectedCraftingItems.Count)
                continue;

            bool match = true;

            foreach (string ingredient in recipe.ingredients)
            {
                if (!selectedCraftingItems.Exists(i => i.itemName == ingredient))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                AddToInventory(recipe.potionName);
                craftedSomething = true;
                break;
            }
        }

        // If no recipe matched  give exactly 1 Junk
        if (!craftedSomething)
        {
            AddToInventory(junkItemName);
        }

        // Consume ingredients
        foreach (InventoryItem item in selectedCraftingItems)
            RemoveFromInventory(item.itemName);

        selectedCraftingItems.Clear();

        RefreshSelectedItemsUI();
        RefreshCraftingUI();
        UpdateInventoryUI();
    }


    #endregion

    #region Sell

    public void OpenSell()
    {
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(true);

        RefreshSellUI();
    }

    void RefreshSellUI()
    {
        ClearChildren(sellItemsParent);

        foreach (InventoryItem item in inventory)
        {
            GameObject btn = Instantiate(buttonPrefab, sellItemsParent);
            btn.GetComponentInChildren<TMP_Text>().text =
                item.itemName + " x" + item.count;

            btn.GetComponent<Button>().onClick.AddListener(() => OnSellClicked(item));
        }
    }

    void SellItem(InventoryItem item, int price)
    {
        coins += price;
        RemoveFromInventory(item.itemName);

        RefreshSellUI();
        UpdateInventoryUI();
        UpdateCoinsUI();
    }

    #endregion

    #region Inventory

    void AddToInventory(string name)
    {
        InventoryItem existing = inventory.Find(i => i.itemName == name);
        if (existing != null) existing.count++;
        else inventory.Add(new InventoryItem { itemName = name, count = 1 });
    }

    void RemoveFromInventory(string name)
    {
        InventoryItem existing = inventory.Find(i => i.itemName == name);
        if (existing == null) return;

        existing.count--;
        if (existing.count <= 0)
            inventory.Remove(existing);
    }

    void UpdateInventoryUI()
    {
        inventoryText.text = "Inventory:\n";
        foreach (InventoryItem item in inventory)
            inventoryText.text += item.itemName + " x" + item.count + "\n";
    }

    void UpdateCoinsUI()
    {
        coinsText.text = "Coins: " + coins;
    }

    void ClearChildren(Transform t)
    {
        foreach (Transform c in t)
            Destroy(c.gameObject);
    }
    public void EndDay()
    {
        StartMarketPhase();
    }

    void OnSellClicked(InventoryItem item)
    {
        // Junk sells instantly
        if (item.itemName == junkItemName)
        {
            SellItem(item, junkSellPrice);
            return;
        }

        pendingSellItem = item;

        sellItemNameText.text = item.itemName;
        priceInputField.text = "";

        sellConfirmPanel.SetActive(true);
    }


    public void ConfirmSell()
    {
        if (pendingSellItem == null)
            return;

        if (!int.TryParse(priceInputField.text, out int price))
            return;

        // Junk ignores min/max
        if (pendingSellItem.itemName == junkItemName)
        {
            SellItem(pendingSellItem, junkSellPrice);
            pendingSellItem = null;
            sellConfirmPanel.SetActive(false);
            return;
        }

        if (!TryGetSellLimits(pendingSellItem.itemName, out int min, out int max))
            return;

        // Silent fail if outside range
        if (price < min || price > max)
            return;

        SellItem(pendingSellItem, price);

        pendingSellItem = null;
        sellConfirmPanel.SetActive(false);
    }



    bool TryGetSellLimits(string itemName, out int min, out int max)
    {
        min = 0;
        max = 0;

        Recipe recipe = recipes.Find(r => r.potionName == itemName);
        if (recipe != null)
        {
            min = recipe.minSellPrice;
            max = recipe.maxSellPrice;
            return true;
        }

        foreach (Market market in markets)
        {
            MarketItem item = market.items.Find(i => i.itemName == itemName);
            if (item != null)
            {
                min = item.minSellPrice;
                max = item.maxSellPrice;
                return true;
            }
        }

        return false;
    }


    #endregion
}
