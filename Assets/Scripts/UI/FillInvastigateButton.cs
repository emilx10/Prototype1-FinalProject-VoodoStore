using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FillInvestigateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image fillImage;
    public Image outLineImage;
    public Button button;

    [HideInInspector] public string itemName; // assigned by GameManager
    public float holdTime = 1f;

    private float timer = 0f;
    private bool isHolding = false;

    private ObjectiveManager objectiveManager;
    private GameManager gameManager;

    private void Start()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();
        if(outLineImage == null)
            outLineImage = GetComponent<Image>();

        fillImage.fillAmount = 0f;

        objectiveManager = FindObjectOfType<ObjectiveManager>();
        gameManager = FindObjectOfType<GameManager>();

        UpdateButtonState();
    }

    private void Update()
    {
        if (objectiveManager == null)
            return;

        UpdateButtonState();

        if (isHolding)
        {
            timer += Time.deltaTime;
            fillImage.fillAmount = timer / holdTime;

            if (timer >= holdTime)
            {
                isHolding = false;
                fillImage.fillAmount = 1f;

                TryInvestigate();
            }
        }
    }

    void UpdateButtonState()
    {
        if (button == null || objectiveManager == null)
            return;

        bool canUse =
            objectiveManager.CanInvestigateToday() &&
            objectiveManager.CanAffordInvestigation();
        if (!canUse)
        {
            Color c = outLineImage.color;
            c.a = 0.5f;
            outLineImage.color = c;
        }
        button.interactable = canUse;
    }

    void TryInvestigate()
    {
        if (!objectiveManager.CanInvestigateToday())
            return;

        if (!objectiveManager.CanAffordInvestigation())
            return;

        bool success = objectiveManager.InvestigateItem(itemName);

        if (success && gameManager != null)
            gameManager.PopulateInventoryPanel();

        ResetFill();
        UpdateButtonState();
    }

    void ResetFill()
    {
        timer = 0f;
        fillImage.fillAmount = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;

        isHolding = true;
        timer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        ResetFill();
    }
}
