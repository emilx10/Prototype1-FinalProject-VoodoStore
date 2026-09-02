using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SellOfferButtonVFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rect;
    private Image image;
    private TMP_Text label;
    private Vector3 targetScale = Vector3.one;
    private Color baseColor;
    private Color glowColor;
    private Color normalTextColor;
    private Color hoverTextColor;
    private float pulseStrength;
    private bool hovered;
    private bool temptFate;
    private float phase;

    public void Configure(Color accent, bool isTemptFate, Color normalText, Color hoverText, float glowAlpha, float idlePulse)
    {
        rect = transform as RectTransform;
        image = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>(true);
        temptFate = isTemptFate;
        normalTextColor = normalText;
        hoverTextColor = hoverText;
        pulseStrength = idlePulse;
        phase = Random.value * 6.28f;
        baseColor = new Color(accent.r, accent.g, accent.b, 0.015f);
        glowColor = new Color(accent.r, accent.g, accent.b, glowAlpha);
        if (image != null) image.color = baseColor;
        if (label != null) label.color = normalTextColor;
    }

    private void Update()
    {
        if (rect == null) return;
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));

        if (image != null)
        {
            float pulse = temptFate ? (Mathf.Sin(Time.unscaledTime * 3.2f + phase) + 1f) * pulseStrength * 0.5f : 0f;
            Color target = hovered ? glowColor : new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a + pulse);
            image.color = Color.Lerp(image.color, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        targetScale = Vector3.one * 1.018f;
        if (label != null) label.color = hoverTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        targetScale = Vector3.one;
        if (label != null) label.color = normalTextColor;
    }

    public void OnPointerDown(PointerEventData eventData) => targetScale = Vector3.one * 0.985f;
    public void OnPointerUp(PointerEventData eventData) => targetScale = hovered ? Vector3.one * 1.018f : Vector3.one;
}
