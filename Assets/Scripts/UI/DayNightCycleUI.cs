using UnityEngine;
using UnityEngine.UI;

public enum DayNightPhase
{
    Day,
    Evening,
    Night
}

public sealed class DayNightCycleUI : MonoBehaviour
{
    private static DayNightCycleUI instance;
    private static DayNightPhase currentPhase = DayNightPhase.Night;
    private static Vector2 currentPosition = new Vector2(0f, -18f);
    private static float currentRotation;
    private static Vector3 currentScale = Vector3.one;

    private DayNightSemicircleGraphic indicator;
    private RectTransform indicatorRect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        EnsureInstance();
    }

    public static void SetPhase(DayNightPhase phase)
    {
        currentPhase = phase;
        EnsureInstance();
        instance.indicator.SetPhase(phase);
    }

    public static void SetLayout(Vector2 anchoredPosition, float rotation, Vector3 scale)
    {
        currentPosition = anchoredPosition;
        currentRotation = rotation;
        currentScale = scale;
        EnsureInstance();
        instance.ApplyLayout();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject(
            "Day Night Cycle UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        DontDestroyOnLoad(root);
        instance = root.AddComponent<DayNightCycleUI>();
        instance.BuildUI();
    }

    private void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject indicatorObject = new GameObject(
            "Cycle Semicircle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(DayNightSemicircleGraphic));
        indicatorObject.transform.SetParent(transform, false);

        indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 1f);
        indicatorRect.anchorMax = new Vector2(0.5f, 1f);
        indicatorRect.pivot = new Vector2(0.5f, 1f);
        indicatorRect.sizeDelta = new Vector2(280f, 140f);
        ApplyLayout();

        indicator = indicatorObject.GetComponent<DayNightSemicircleGraphic>();
        indicator.raycastTarget = false;
        indicator.SetPhase(currentPhase);
    }

    private void ApplyLayout()
    {
        if (indicatorRect == null)
            return;

        indicatorRect.anchoredPosition = currentPosition;
        indicatorRect.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
        indicatorRect.localScale = currentScale;
    }
}

public sealed class DayNightSemicircleGraphic : MaskableGraphic
{
    private const int StepsPerSegment = 18;
    private static readonly Color DayColor = new Color(1f, 0.84f, 0.12f, 1f);
    private static readonly Color EveningColor = new Color(1f, 0.36f, 0.06f, 1f);
    private static readonly Color NightColor = new Color(0.015f, 0.02f, 0.04f, 1f);
    private static readonly Color BorderColor = new Color(0.82f, 0.82f, 0.78f, 0.8f);

    private DayNightPhase phase = DayNightPhase.Night;

    public void SetPhase(DayNightPhase newPhase)
    {
        phase = newPhase;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = new Vector2(rect.center.x, rect.yMin);
        float outerRadius = Mathf.Min(rect.width * 0.46f, rect.height * 0.92f);

        AddArc(vertexHelper, center, outerRadius + 5f, outerRadius - 2f, 0f, 180f, BorderColor, 54);

        DrawPhaseSegment(vertexHelper, center, DayNightPhase.Day, DayColor, 122f, 178f, outerRadius);
        DrawPhaseSegment(vertexHelper, center, DayNightPhase.Evening, EveningColor, 62f, 118f, outerRadius);
        DrawPhaseSegment(vertexHelper, center, DayNightPhase.Night, NightColor, 2f, 58f, outerRadius);
    }

    private void DrawPhaseSegment(
        VertexHelper vertexHelper,
        Vector2 center,
        DayNightPhase segmentPhase,
        Color segmentColor,
        float startAngle,
        float endAngle,
        float outerRadius)
    {
        bool isActive = phase == segmentPhase;
        float radius = isActive ? outerRadius : outerRadius * 0.86f;
        Color color = segmentColor;
        color.a = isActive ? 1f : 0.42f;

        AddPieSegment(
            vertexHelper,
            center,
            radius,
            startAngle,
            endAngle,
            color,
            StepsPerSegment);
    }

    private static void AddPieSegment(
        VertexHelper vertexHelper,
        Vector2 center,
        float radius,
        float startAngle,
        float endAngle,
        Color color,
        int steps)
    {
        int centerIndex = vertexHelper.currentVertCount;
        vertexHelper.AddVert(center, color, Vector2.zero);

        for (int step = 0; step <= steps; step++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, step / (float)steps) * Mathf.Deg2Rad;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertexHelper.AddVert(point, color, Vector2.zero);

            if (step > 0)
                vertexHelper.AddTriangle(centerIndex, centerIndex + step, centerIndex + step + 1);
        }
    }

    private static void AddArc(
        VertexHelper vertexHelper,
        Vector2 center,
        float outerRadius,
        float innerRadius,
        float startAngle,
        float endAngle,
        Color color,
        int steps)
    {
        int startIndex = vertexHelper.currentVertCount;

        for (int step = 0; step <= steps; step++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, step / (float)steps) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vertexHelper.AddVert(center + direction * outerRadius, color, Vector2.zero);
            vertexHelper.AddVert(center + direction * innerRadius, color, Vector2.zero);

            if (step == 0)
                continue;

            int previousOuter = startIndex + (step - 1) * 2;
            int previousInner = previousOuter + 1;
            int currentOuter = startIndex + step * 2;
            int currentInner = currentOuter + 1;

            vertexHelper.AddTriangle(previousOuter, currentOuter, currentInner);
            vertexHelper.AddTriangle(previousOuter, currentInner, previousInner);
        }
    }
}
