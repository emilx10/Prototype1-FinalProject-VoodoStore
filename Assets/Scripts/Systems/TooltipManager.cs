using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public GameObject container;
    public TMP_Text tooltipText;

    RectTransform rect;
    RectTransform canvasRect;
    Camera cam;

    void Awake()
    {
        Instance = this;

        rect = container.GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        cam = Camera.main; // camera rendering the world space canvas

        container.SetActive(false);
    }

    void Update()
    {
        Vector2 pos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            cam,
            out pos
        );

        rect.anchoredPosition = pos;
    }

    public void Show(string text)
    {
        tooltipText.text = text;
        container.SetActive(true);
    }

    public void Hide()
    {
        container.SetActive(false);
    }
}