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

    public Sprite ItemIcon;

    [TextArea(2, 5)]
    public string description;

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
    public Sprite ItemIcon;
    public string description;
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
    [SerializeField] float vol, pitch;

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

    public static UnityAction OnItemBought;
    public static UnityAction OnSuccessfulMerge;
    public static UnityAction OnFailedMerge;
    public static UnityAction<bool> OnItemSold;
    public static UnityAction<string, ItemCategory> OnItemAdded;

    private InventoryItem pendingSellItem;
    private List<InventoryItem> inventory = new List<InventoryItem>();
    private List<InventoryItem> selectedCraftingItems = new List<InventoryItem>();

    public ObjectiveManager objectiveManager;

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

        ClearChildren(itemsButtonsParent);

        foreach (MarketItem item in market.items)
        {
            GameObject btn = Instantiate(buttonPrefab, itemsButtonsParent);

            var tooltip = btn.GetComponent<ItemHoverTooltip>();
            tooltip.marketItem = item;

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
        AddToInventory(item.itemName, item.category, item.description, item.ItemIcon);

        PopulateInventoryPanel();
        UpdateCoinsUI();

        OnItemBought?.Invoke();
    }

    void AddToInventory(string name, ItemCategory category, string description = "", Sprite icon = null)
    {
        InventoryItem existing = inventory.Find(i => i.itemName == name);

        if (existing != null)
        {
            existing.count++;
            if (existing.ItemIcon == null && icon != null)
                existing.ItemIcon = icon;
        }
        else
        {
            inventory.Add(new InventoryItem
            {
                itemName = name,
                count = 1,
                category = category,
                description = description,
                ItemIcon = icon
            });
        }

        OnItemAdded?.Invoke(name, category);
    }

    public void PopulateInventoryPanel()
    {
        foreach (Transform child in inventoryListParent)
            Destroy(child.gameObject);

        foreach (InventoryItem item in inventory)
        {
            GameObject row = Instantiate(inventoryRowPrefab, inventoryListParent);

            TMP_Text itemNameText = row.transform.Find("ItemNameText").GetComponent<TMP_Text>();
            TMP_Text itemCountText = row.transform.Find("ItemCountText").GetComponent<TMP_Text>();
            Image icon = row.transform.Find("Icon").GetComponent<Image>();

            itemNameText.text = item.itemName;
            itemCountText.text = "x" + item.count;

            if (item.ItemIcon != null)
                icon.sprite = item.ItemIcon;
        }
    }

    void OnPriceSliderChanged(float value)
    {
        priceValueText.text = Mathf.RoundToInt(value).ToString();
    }

    public void UpdateCoinsUI()
    {
        coinsText.text = $"{coins}";
    }

    void ApplyCategoryStyle(GameObject button, ItemCategory category)
    {
        if (!TryGetCategoryStyle(category, out var style)) return;

        Image bg = button.GetComponent<Image>();
        TMP_Text txt = button.GetComponentInChildren<TMP_Text>();

        if (bg != null) bg.color = style.backgroundColor;
        if (txt != null) txt.color = style.textColor;
    }

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

    void ClearChildren(Transform t)
    {
        foreach (Transform c in t)
            Destroy(c.gameObject);
    }
}