using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public InventoryItem inventoryItem; // For crafting/sell
    public MarketItem marketItem;       // For shop

    public void OnPointerEnter(PointerEventData eventData)
    {
        string text = "";

        if (marketItem != null)
            text = $"{marketItem.itemName}\n{marketItem.price} coins\n{marketItem.description}";
        else if (inventoryItem != null)
            text = $"{inventoryItem.itemName}\n{inventoryItem.count} x\n{inventoryItem.description}";

        TooltipManager.Instance.Show(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
}