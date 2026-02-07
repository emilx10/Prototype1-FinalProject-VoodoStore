using UnityEngine;
using UnityEngine.UI;

public class FillInvestigateButton : MonoBehaviour
{
    public Image fillImage;
    public float holdTime = 1f;

    private float timer = 0f;
    private bool isHolding = false;

    private void Start()
    {
        fillImage = GetComponent<Image>();
        fillImage.fillAmount = 0f;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isHolding = true;
            timer = 0f;
        }

        if (Input.GetMouseButton(0) && isHolding)
        {
            timer += Time.deltaTime;
            fillImage.fillAmount = timer / holdTime;

            if (timer >= holdTime)
            {
                fillImage.fillAmount = 1f;
                isHolding = false;

                // Action after full hold
                Debug.Log("Investigate complete");
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isHolding = false;
            timer = 0f;
            fillImage.fillAmount = 0f;
        }
    }
}
