using UnityEngine;
using UnityEngine.UI;

public class ButtonBreather : MonoBehaviour
{
    public float speed = 2f;          // How fast it breathes
    public float scaleAmount = 0.05f; // How big the pulse is

    private Vector3 originalScale;
    private bool isBreathing = true;

    private Button button;
    public bool playOnStart = false;
    void Start()
    {
        originalScale = transform.localScale;

        if (playOnStart)
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

    public void StopBreathing()
    {
        isBreathing = false;
        transform.localScale = originalScale;
    }

    public void StartBreathing()
    {
        isBreathing = true;
    }
}