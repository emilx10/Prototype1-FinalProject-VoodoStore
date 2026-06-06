
using UnityEngine;
using UnityEngine.EventSystems;

public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject ScaleObject;
    public float scaleMultiplier = 1.1f;
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        originalScale = ScaleObject.transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        ScaleObject.transform.localScale = Vector3.Lerp(ScaleObject.transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * scaleMultiplier;

        AudioManager.Instance.PlaySfx(0.7f, SFX.UI_Button_Hover, 1);
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}
