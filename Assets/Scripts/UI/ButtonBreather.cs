using UnityEngine;
using UnityEngine.UI;

public class ButtonBreather : MonoBehaviour
{
    public float speed = 2f;          // How fast it breathes
    public float scaleAmount = 0.05f; // How big the pulse is

    private Vector3 originalScale;
    private bool isBreathing = true;
    private bool isPaused;
    public bool IsBreathing => isBreathing;

    private Button button;
    public bool playOnStart = false;
    private void Awake()
    {
        originalScale = transform.localScale;
    }

    void Start()
    {
        if (playOnStart && !isPaused)
            StartBreathing();
        else
            StopBreathing();

        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(StopBreathing);
        }
    }

    void Update()
    {
        if (!isBreathing) return;

        float scaleOffset = Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = originalScale + Vector3.one * scaleOffset;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(StopBreathing);
    }

    public void StopBreathing()
    {
        isBreathing = false;
        transform.localScale = originalScale;
    }

    public void StartBreathing()
    {
        if (isPaused)
            return;

        isBreathing = true;
    }

    public void PauseBreathing()
    {
        isPaused = true;
        StopBreathing();
    }

    public void ResumeBreathing()
    {
        isPaused = false;
        StartBreathing();
    }
}
