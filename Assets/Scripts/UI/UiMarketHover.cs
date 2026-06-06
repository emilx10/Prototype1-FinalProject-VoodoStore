using System.Collections;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiMarketHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private float duration = 0.25f;

    [SerializeField] private SFX soundEffect;
    [SerializeField] private float pitch = 1f;
    
    private Material targetMaterial;
    private Coroutine transitionCoroutine;
    private static readonly int AppearProperty = Shader.PropertyToID("_Appear");

    private void Awake()
    {
        if (targetObject == null) return;

        if (targetObject.TryGetComponent<Graphic>(out var graphic))
        {
            targetMaterial = graphic.materialForRendering;
        }
        else if (targetObject.TryGetComponent<Renderer>(out var renderer))
        {
            targetMaterial = renderer.material;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySfx(0.4f, soundEffect, pitch);
        
        AudioManager.Instance.PlaySfx(0.7f, SFX.UI_Button_Hover, 1);
        
        TriggerTransition(1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TriggerTransition(0f);
    }

    private void TriggerTransition(float targetValue)
    {
        if (targetMaterial == null || !targetMaterial.HasProperty(AppearProperty)) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(AnimateAppear(targetValue));
    }

    private IEnumerator AnimateAppear(float targetValue)
    {
        float startValue = targetMaterial.GetFloat(AppearProperty);
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, time / duration);
            targetMaterial.SetFloat(AppearProperty, newValue);
            yield return null;
        }

        targetMaterial.SetFloat(AppearProperty, targetValue);
    }
}
