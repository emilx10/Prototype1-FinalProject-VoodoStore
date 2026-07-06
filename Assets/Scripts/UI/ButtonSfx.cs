using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float Volume = 0.1f;
    private const float HoverPitch = 1.2f;
    private const float ClickPitch = 1f;
    private const float HoverScale = 1.1f;
    private const float ScaleSpeed = 14f;

    private Button button;
    private Vector3 originalScale;
    private Vector3 targetScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstaller()
    {
        if (FindFirstObjectByType<ButtonSfxInstaller>() != null)
        {
            return;
        }

        GameObject installer = new GameObject("Button SFX Installer");
        DontDestroyOnLoad(installer);
        installer.AddComponent<ButtonSfxInstaller>();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClick);
        }

        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanInteract())
        {
            targetScale = originalScale * HoverScale;
        }

        if (CanPlay())
        {
            AudioManager.Instance.PlaySfx(Volume, SFX.SFX_Hover, HoverPitch);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    private void PlayClick()
    {
        if (CanPlay())
        {
            AudioManager.Instance.PlaySfx(Volume, SFX.SFX_Click, ClickPitch);
        }
    }

    private bool CanPlay()
    {
        return CanInteract() && AudioManager.Instance != null;
    }

    private bool CanInteract()
    {
        return button != null && button.IsInteractable();
    }
}

internal sealed class ButtonSfxInstaller : MonoBehaviour
{
    private const float ScanInterval = 0.25f;

    private Coroutine scanRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        scanRoutine = StartCoroutine(ScanButtons());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToButtons();
    }

    private IEnumerator ScanButtons()
    {
        WaitForSeconds wait = new WaitForSeconds(ScanInterval);

        while (true)
        {
            AttachToButtons();
            yield return wait;
        }
    }

    private static void AttachToButtons()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponent<ButtonSfx>() == null)
            {
                buttons[i].gameObject.AddComponent<ButtonSfx>();
            }
        }
    }
}
