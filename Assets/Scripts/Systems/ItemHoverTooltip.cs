using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public InventoryItem inventoryItem; // For crafting/sell
    public MarketItem marketItem;       // For shop

    public void OnPointerEnter(PointerEventData eventData)
    {
        string text = "";

        if (marketItem != null && !string.IsNullOrEmpty(marketItem.itemName))
        {
            text = $"{marketItem.itemName}\n{marketItem.price} coins\n{marketItem.description}";
        }
        else if (inventoryItem != null && !string.IsNullOrEmpty(inventoryItem.itemName))
        {
            text = $"{inventoryItem.itemName}\n{inventoryItem.count} x\n{inventoryItem.description}";
        }

        if (!string.IsNullOrEmpty(text))
        {
            // Pass the RectTransform of the UI element this script is on
            TooltipManager.Instance.Show(text);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
}