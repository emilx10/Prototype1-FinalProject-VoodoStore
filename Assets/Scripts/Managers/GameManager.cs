using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MarketItem
{
    public string itemName;
    public int price;
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
    public List<string> ingredients; // Names of items needed
    public int sellPrice = 10; // <-- NEW: editable in Inspector
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
    public GameObject buttonPrefab;

    [Header("Game Data")]
    public List<Market> markets;
    public List<Recipe> recipes;
    public int coins = 100;

    private List<InventoryItem> potions = new List<InventoryItem>(); // track counts at runtime
    private Market currentMarket;

    void Start()
    {
        StartMarketPhase();
        UpdateInventoryUI();
        UpdateCoinsUI();
    }

    #region Market Phase
    public void StartMarketPhase()
    {
        marketPanel.SetActive(true);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(false);
        sellPanel.SetActive(false);

        foreach (Transform t in marketButtonsParent)
            Destroy(t.gameObject);

        foreach (Market market in markets)
        {
            GameObject btnObj = Instantiate(buttonPrefab, marketButtonsParent);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = market.marketName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OpenMarket(market));
        }
    }

    public void OpenMarket(Market market)
    {
        currentMarket = market;
        marketPanel.SetActive(false);
        itemsPanel.SetActive(true);

        foreach (Transform t in itemsButtonsParent)
            Destroy(t.gameObject);

        foreach (MarketItem item in market.items)
        {
            GameObject btnObj = Instantiate(buttonPrefab, itemsButtonsParent);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = item.itemName + " - " + item.price + " coins";

            btnObj.GetComponent<Button>().onClick.AddListener(() => BuyItem(item));
        }
    }

    void BuyItem(MarketItem item)
    {
        if (coins >= item.price)
        {
            coins -= item.price;
            AddToInventory(item.itemName);
            UpdateInventoryUI();
            UpdateCoinsUI();
        }
    }
    #endregion

    #region Crafting Phase
    public void OpenCrafting()
    {
        marketPanel.SetActive(false);
        itemsPanel.SetActive(false);
        craftingPanel.SetActive(true);
        sellPanel.SetActive(false);

        RefreshCraftingUI();
    }

    void RefreshCraftingUI()
    {
        foreach (Transform t in craftingItemsParent)
            Destroy(t.gameObject);

        foreach (InventoryItem item in potions)
        {
            GameObject btnObj = Instantiate(buttonPrefab, craftingItemsParent);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = item.itemName + " x" + item.count;
            btnObj.GetComponent<Button>().onClick.AddListener(() => SelectCraftingItem(item));
        }
    }

    private List<InventoryItem> selectedCraftingItems = new List<InventoryItem>();

    void SelectCraftingItem(InventoryItem item)
    {
        if (!selectedCraftingItems.Contains(item) && selectedCraftingItems.Count < 3)
        {
            selectedCraftingItems.Add(item);
        }
    }

    public void MergeItems()
    {
        if (selectedCraftingItems.Count != 3) return;

        foreach (Recipe recipe in recipes)
        {
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
                AddPotion(recipe.potionName);
                break;
            }
        }

        // Remove materials
        foreach (InventoryItem item in selectedCraftingItems)
            RemoveFromInventory(item.itemName);

        selectedCraftingItems.Clear();
        RefreshCraftingUI();
        UpdateInventoryUI();
    }
    #endregion

    #region Sell Phase
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
        foreach (Transform t in sellItemsParent)
            Destroy(t.gameObject);

        foreach (InventoryItem potion in potions)
        {
            Recipe recipe = recipes.Find(r => r.potionName == potion.itemName);
            int price = recipe != null ? recipe.sellPrice : 10;

            GameObject btnObj = Instantiate(buttonPrefab, sellItemsParent);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = potion.itemName + " x" + potion.count + " - " + price + " coins";

            btnObj.GetComponent<Button>().onClick.AddListener(() => SellPotion(potion, price));
        }
    }

    void SellPotion(InventoryItem potion, int price)
    {
        coins += price;
        RemovePotion(potion.itemName);
        RefreshSellUI();
        UpdateCoinsUI();
    }

    public void EndDay()
    {
        StartMarketPhase();
    }
    #endregion

    #region Inventory Management
    void AddToInventory(string itemName)
    {
        InventoryItem existing = potions.Find(i => i.itemName == itemName);
        if (existing != null)
            existing.count++;
        else
            potions.Add(new InventoryItem() { itemName = itemName, count = 1 });
    }

    void RemoveFromInventory(string itemName)
    {
        InventoryItem existing = potions.Find(i => i.itemName == itemName);
        if (existing != null)
        {
            existing.count--;
            if (existing.count <= 0) potions.Remove(existing);
        }
    }

    void AddPotion(string potionName)
    {
        InventoryItem existing = potions.Find(p => p.itemName == potionName);
        if (existing != null)
            existing.count++;
        else
            potions.Add(new InventoryItem() { itemName = potionName, count = 1 });
    }

    void RemovePotion(string potionName)
    {
        InventoryItem existing = potions.Find(p => p.itemName == potionName);
        if (existing != null)
        {
            existing.count--;
            if (existing.count <= 0) potions.Remove(existing);
        }
    }

    void UpdateInventoryUI()
    {
        inventoryText.text = "Inventory:\n";
        foreach (InventoryItem item in potions)
            inventoryText.text += item.itemName + " x" + item.count + "\n";
    }

    void UpdateCoinsUI()
    {
        coinsText.text = "Coins: " + coins;
    }
    #endregion
}
