using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MarketItem
{
    public string itemName;
    public int price;      // buy price
    public int sellPrice;  // sell price (Inspector)
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
    public int sellPrice; // potion sell price
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

    [Header("Crafting Selection UI")]
    public Transform selectedItemsParent;
    public GameObject selectedItemTextPrefab;

    public GameObject buttonPrefab;

    [Header("Game Data")]
    public List<Market> markets;
    public List<Recipe> recipes;
    public int coins = 100;

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

        foreach (InventoryItem item in selectedCraftingItems)
        {
            GameObject txt = Instantiate(selectedItemTextPrefab, selectedItemsParent);
            txt.GetComponent<TMP_Text>().text = item.itemName;
        }
    }

    public void MergeItems()
    {
        if (selectedCraftingItems.Count < 2) return;

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
                break;
            }
        }

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
            int price = GetSellPrice(item.itemName);

            GameObject btn = Instantiate(buttonPrefab, sellItemsParent);
            btn.GetComponentInChildren<TMP_Text>().text =
                item.itemName + " x" + item.count + " - " + price + " coins";

            btn.GetComponent<Button>().onClick.AddListener(() => SellItem(item, price));
        }
    }

    int GetSellPrice(string itemName)
    {
        Recipe recipe = recipes.Find(r => r.potionName == itemName);
        if (recipe != null)
            return recipe.sellPrice;

        foreach (Market market in markets)
        {
            MarketItem item = market.items.Find(i => i.itemName == itemName);
            if (item != null)
                return item.sellPrice;
        }

        return 0;
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

    #endregion
}
