using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateItemButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Button btn;

    // ------------------- MAIN UPDATE -------------------
    public void UpdateItemData(string itemName, Sprite itemIcon)
    {
        if (itemIcon == null)
        {
            if (text != null)
                text.text = itemName;

            if (icon != null)
                icon.gameObject.SetActive(false);
        }
        else
        {
            if (text != null)
                text.text = "";

            if (icon != null)
            {
                icon.sprite = itemIcon;
                icon.gameObject.SetActive(true);
            }
        }
    }

    // ------------------- APPLY STYLE FROM OUTSIDE -------------------
    public void ApplyStyle(Color bgColor, Color textColor)
    {
        if (background != null)
            background.color = bgColor;

        if (text != null)
            text.color = textColor;
    }

    public Button GetButton()
    {
        return btn;
    }
}