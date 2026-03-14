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

    [SerializeField] Vector2 mouseOffset = new Vector2(40, -40); // right + down

    void Awake()
    {
        Instance = this;

        rect = container.GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        cam = Camera.main;

        container.SetActive(false);
    }

    void Update()
    {
        if (!container.activeSelf) return;

        Vector2 pos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            cam,
            out pos
        );

        rect.anchoredPosition = pos + mouseOffset;
    }

    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        tooltipText.text = text;
        container.SetActive(true);
    }

    public void Hide()
    {
        container.SetActive(false);
    }
}