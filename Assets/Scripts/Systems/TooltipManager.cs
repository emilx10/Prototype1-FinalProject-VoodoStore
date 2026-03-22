using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public GameObject container;
    public TMP_Text tooltipText;

    private RectTransform rect;
    [SerializeField] private Vector3 mouseOffset = new Vector3(0.5f, -0.5f, 0f); // offset from mouse in world units

    void Awake()
    {
        Instance = this;
        rect = container.GetComponent<RectTransform>();
        container.SetActive(false);
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