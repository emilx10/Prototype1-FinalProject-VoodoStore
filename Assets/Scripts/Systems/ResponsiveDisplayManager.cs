using UnityEngine;
using UnityEngine.UI;

public sealed class ResponsiveDisplayManager : MonoBehaviour
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private const float TargetAspect = 16f / 9f;
    private const float MatchWidthOrHeight = 0.5f;

    private int lastScreenWidth;
    private int lastScreenHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (FindFirstObjectByType<ResponsiveDisplayManager>() != null)
        {
            return;
        }

        GameObject instance = new GameObject("Responsive Display Manager");
        DontDestroyOnLoad(instance);
        instance.AddComponent<ResponsiveDisplayManager>();
    }

    private void Awake()
    {
        ApplyResponsiveSettings();
    }

    private void Update()
    {
        if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
        {
            return;
        }

        ApplyResponsiveSettings();
    }

    private void ApplyResponsiveSettings()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        ConfigureStandaloneWindow();
        ConfigureCameras();
        ConfigureCanvasScalers();
        Canvas.ForceUpdateCanvases();
    }

    private static void ConfigureStandaloneWindow()
    {
#if UNITY_STANDALONE
        if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
#endif
    }

    private static void ConfigureCameras()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Camera camera in cameras)
        {
            if (camera == null || camera.targetTexture != null)
            {
                continue;
            }

            ApplyTargetAspect(camera);
        }
    }

    private static void ApplyTargetAspect(Camera camera)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        float screenAspect = (float)Screen.width / Screen.height;
        Rect viewport = new Rect(0f, 0f, 1f, 1f);

        if (screenAspect > TargetAspect)
        {
            float width = TargetAspect / screenAspect;
            viewport.x = (1f - width) * 0.5f;
            viewport.width = width;
        }
        else if (screenAspect < TargetAspect)
        {
            float height = screenAspect / TargetAspect;
            viewport.y = (1f - height) * 0.5f;
            viewport.height = height;
        }

        camera.rect = viewport;
    }

    private static void ConfigureCanvasScalers()
    {
        CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (CanvasScaler scaler in scalers)
        {
            if (scaler == null)
            {
                continue;
            }

            ConfigureCanvasScaler(scaler);
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            if (canvas.GetComponent<CanvasScaler>() != null)
            {
                continue;
            }

            ConfigureCanvasScaler(canvas.gameObject.AddComponent<CanvasScaler>());
        }
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        Canvas canvas = scaler.GetComponent<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 1f;
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = MatchWidthOrHeight;
        scaler.referencePixelsPerUnit = 100f;
    }
}
