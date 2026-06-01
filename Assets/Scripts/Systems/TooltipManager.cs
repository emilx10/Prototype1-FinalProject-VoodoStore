using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    private const int TooltipSortingOrder = 200;

    public static TooltipManager Instance;

    public GameObject container;
    public TMP_Text tooltipText;

    private RectTransform rect;
    [SerializeField] private Vector3 mouseOffset = new Vector3(0.5f, -0.5f, 0f); // offset from mouse in world units

    void Awake()
    {
        Instance = this;
        rect = container.GetComponent<RectTransform>();
        EnsureTooltipCanvas();
        container.SetActive(false);
    }

    private void EnsureTooltipCanvas()
    {
        if (container == null) return;

        Canvas canvas = container.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = container.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = TooltipSortingOrder;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    void Update()
    {
        if (!container.activeSelf) return;

        // Convert mouse screen position to world position
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Set Z to match the tooltip's canvas (so it renders properly)
        mouseWorldPos.z = rect.position.z;

        // Apply offset
        rect.position = mouseWorldPos + mouseOffset;
    }

    /// <summary>
    /// Show the tooltip with a text
    /// </summary>
    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        tooltipText.text = text;
        container.SetActive(true);
    }

    /// <summary>
    /// Hide the tooltip
    /// </summary>
    public void Hide()
    {
        container.SetActive(false);
    }
}
