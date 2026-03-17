using System.Collections;
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

    private Material bgMaterialInstance;
    private Material iconMaterialInstance;

    // ------------------- INIT MATERIAL INSTANCES -------------------
    void Awake()
    {
        // Clone material for background
        if (background != null && background.material != null)
        {
            bgMaterialInstance = new Material(background.material);
            background.material = bgMaterialInstance;
        }

        // Clone material for icon
        if (icon != null && icon.material != null)
        {
            iconMaterialInstance = new Material(icon.material);
            icon.material = iconMaterialInstance;
        }
    }

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

    // ------------------- APPLY STYLE -------------------
    public void ApplyStyle(Color bgColor, Color textColor)
    {
        if (background != null)
            background.color = bgColor;

        if (text != null)
            text.color = textColor;
    }

    // ------------------- DISSOLVE ANIMATION -------------------
    public void PlayDissolve(float duration = 0.5f)
    {
        StartCoroutine(DissolveRoutine(duration));
    }

    IEnumerator DissolveRoutine(float duration)
    {
        float time = 0f;

        float start = -2f;
        float end = 1f;

        while (time < duration)
        {
            float t = time / duration;
            float value = Mathf.Lerp(start, end, t);

            SetLifeTime(value);

            time += Time.deltaTime;
            yield return null;
        }

        SetLifeTime(end);
    }

    void SetLifeTime(float value)
    {
        if (bgMaterialInstance != null)
            bgMaterialInstance.SetFloat("_LifeTime", value);

        if (iconMaterialInstance != null)
            iconMaterialInstance.SetFloat("_LifeTime", value);
    }

    public Button GetButton()
    {
        return btn;
    }
}