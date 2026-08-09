using System.Collections;
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
    private const int SortingOrder = 420;

    private static DayNightCycleUI instance;
    private static DayNightPhase currentPhase = DayNightPhase.Night;

    public static DayNightPhase CurrentPhase => currentPhase;
    public static event System.Action<DayNightPhase> PhaseChanged;

    [Header("Your Scene UI")]
    [SerializeField] private RectTransform sceneClockRoot;
    [SerializeField] private Image sceneArrow;

    [Header("Phase Arrow Copies")]
    [SerializeField] private GameObject dayArrow;
    [SerializeField] private GameObject eveningArrow;
    [SerializeField] private GameObject nightArrow;

    [Header("Your Phase Targets")]
    [SerializeField] private RectTransform dayTarget;
    [SerializeField] private RectTransform eveningTarget;
    [SerializeField] private RectTransform nightTarget;

    [Header("Arrow Tip")]
    [Tooltip("Place a child RectTransform on the visible tip of ClockArrow and assign it here. The script rotates ClockArrow so this child points at the phase target.")]
    [SerializeField] private RectTransform arrowTipTransform;

    private RectTransform arrowRect;
    private Coroutine arrowAnimation;
    private float currentArrowRotation;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        Initialize();
        ApplyPhase(currentPhase, true);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void SetPhase(DayNightPhase phase)
    {
        SetPhase(phase, false);
    }

    public static void SetPhase(DayNightPhase phase, bool instant)
    {
        currentPhase = phase;
        PhaseChanged?.Invoke(phase);

        if (!EnsureSceneInstance())
            return;

        instance.ApplyPhase(phase, instant);
    }

    public static void SetLayout(Vector2 anchoredPosition, Vector2 size, float rotation, Vector3 scale)
    {
        EnsureSceneInstance();
    }

    public static void SetLayout(Vector2 anchoredPosition, float rotation, Vector3 scale)
    {
        EnsureSceneInstance();
    }

    public static void SetPartLayout(
        Vector2 facePosition,
        Vector2 faceSize,
        Vector3 faceScale,
        float faceRotation,
        Vector2 circlePosition,
        Vector2 circleSize,
        Vector3 circleScale,
        float circleRotation,
        Vector2 arrowPosition,
        Vector2 arrowSize,
        Vector2 arrowPivot,
        Vector3 arrowScale)
    {
        EnsureSceneInstance();
    }

    private static bool EnsureSceneInstance()
    {
        if (instance != null)
            return true;

        instance = FindFirstObjectByType<DayNightCycleUI>();
        if (instance == null)
        {
            Debug.LogError("DayNightCycleUI scene object is missing. Add it to your DayNightClock UI object.");
            return false;
        }

        instance.Initialize();
        return true;
    }

    private void Initialize()
    {
        if (sceneClockRoot == null)
            sceneClockRoot = transform as RectTransform;

        if (sceneArrow != null)
            arrowRect = sceneArrow.rectTransform;

        if (arrowTipTransform == null)
            arrowTipTransform = FindArrowTipChild();

        if (arrowRect != null)
        {
            currentArrowRotation = NormalizeAngle(arrowRect.localEulerAngles.z);
        }

        FindPhaseArrowCopies();

        EnsureClockDrawsAboveMarket();
    }

    private void EnsureClockDrawsAboveMarket()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
    }

    private void ApplyPhase(DayNightPhase phase, bool instant)
    {
        if (sceneClockRoot == null)
            return;

        if (arrowAnimation != null)
        {
            StopCoroutine(arrowAnimation);
            arrowAnimation = null;
        }

        if (HasPhaseArrowCopies())
        {
            ApplyPhaseArrowVisibility(phase);
            return;
        }

        if (arrowRect == null)
            return;

        float targetRotation = GetRotationForPhase(phase);

        if (instant)
        {
            ApplyArrowRotation(targetRotation);
            return;
        }

        arrowAnimation = StartCoroutine(AnimateArrowTo(targetRotation));
    }

    private IEnumerator AnimateArrowTo(float targetRotation)
    {
        float startRotation = currentArrowRotation;
        float delta = Mathf.DeltaAngle(startRotation, targetRotation);
        float duration = Mathf.Clamp(Mathf.Abs(delta) / 180f * 0.7f, 0.28f, 0.7f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            ApplyArrowRotation(startRotation + delta * eased);
            yield return null;
        }

        ApplyArrowRotation(targetRotation);
        arrowAnimation = null;
    }

    private float GetRotationForPhase(DayNightPhase phase)
    {
        RectTransform target = GetTargetForPhase(phase);
        if (target == null || arrowRect == null || sceneClockRoot == null)
            return currentArrowRotation;

        Vector2 targetPoint = GetLocalPoint(sceneClockRoot, target);
        Vector2 arrowPivotPoint = arrowRect.anchoredPosition;
        Vector2 targetDirection = targetPoint - arrowPivotPoint;
        Vector2 tip = GetArrowTipLocalPoint();

        if (targetDirection.sqrMagnitude < 0.001f || tip.sqrMagnitude < 0.001f)
            return currentArrowRotation;

        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        float tipAngle = Mathf.Atan2(tip.y, tip.x) * Mathf.Rad2Deg;
        return NormalizeAngle(targetAngle - tipAngle);
    }

    private RectTransform GetTargetForPhase(DayNightPhase phase)
    {
        switch (phase)
        {
            case DayNightPhase.Day:
                return dayTarget;
            case DayNightPhase.Evening:
                return eveningTarget;
            case DayNightPhase.Night:
                return nightTarget;
            default:
                return nightTarget;
        }
    }

    private RectTransform FindArrowTipChild()
    {
        if (arrowRect == null)
            return null;

        RectTransform exact = arrowRect.Find("ArrowTip") as RectTransform;
        if (exact != null)
            return exact;

        exact = arrowRect.Find("Arrow Tip") as RectTransform;
        if (exact != null)
            return exact;

        exact = arrowRect.Find("Tip") as RectTransform;
        if (exact != null)
            return exact;

        for (int i = 0; i < arrowRect.childCount; i++)
        {
            if (arrowRect.GetChild(i) is RectTransform child &&
                child.name.ToLowerInvariant().Contains("tip"))
            {
                return child;
            }
        }

        return null;
    }

    private void FindPhaseArrowCopies()
    {
        if (sceneClockRoot == null)
            return;

        if (dayArrow == null)
            dayArrow = FindChildGameObject("DayArrow", "Day Arrow", "ClockArrowDay");

        if (eveningArrow == null)
            eveningArrow = FindChildGameObject("EveningArrow", "Evening Arrow", "EveArrow", "Eve Arrow", "ClockArrowEvening");

        if (nightArrow == null)
            nightArrow = FindChildGameObject("NightArrow", "Night Arrow", "ClockArrowNight");
    }

    private GameObject FindChildGameObject(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = sceneClockRoot.Find(names[i]);
            if (child != null)
                return child.gameObject;
        }

        return null;
    }

    private bool HasPhaseArrowCopies()
    {
        return dayArrow != null || eveningArrow != null || nightArrow != null;
    }

    private void ApplyPhaseArrowVisibility(DayNightPhase phase)
    {
        SetArrowCopyActive(dayArrow, phase == DayNightPhase.Day);
        SetArrowCopyActive(eveningArrow, phase == DayNightPhase.Evening);
        SetArrowCopyActive(nightArrow, phase == DayNightPhase.Night);

        if (sceneArrow != null &&
            sceneArrow.gameObject != dayArrow &&
            sceneArrow.gameObject != eveningArrow &&
            sceneArrow.gameObject != nightArrow)
        {
            sceneArrow.gameObject.SetActive(false);
        }
    }

    private static void SetArrowCopyActive(GameObject arrow, bool active)
    {
        if (arrow != null && arrow.activeSelf != active)
            arrow.SetActive(active);
    }

    private static Vector2 GetLocalPoint(RectTransform root, RectTransform target)
    {
        Vector3 worldPoint = target.TransformPoint(target.rect.center);
        return root.InverseTransformPoint(worldPoint);
    }

    private Vector2 GetArrowTipLocalPoint()
    {
        if (arrowTipTransform == null || arrowRect == null)
        {
            Debug.LogWarning("DayNightCycleUI needs an ArrowTip child assigned under ClockArrow.");
            return Vector2.zero;
        }

        Vector3 worldTipPoint = arrowTipTransform.TransformPoint(arrowTipTransform.rect.center);
        return arrowRect.InverseTransformPoint(worldTipPoint);
    }

    private void ApplyArrowRotation(float rotation)
    {
        currentArrowRotation = NormalizeAngle(rotation);
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, currentArrowRotation);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle <= -180f)
            angle += 360f;

        return angle;
    }
}
