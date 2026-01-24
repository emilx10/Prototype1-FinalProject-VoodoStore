using TMPro;
using UnityEngine;

public class FloatingCoinText : MonoBehaviour
{
    public float floatSpeed = 50f;
    public float lifetime = 1f;

    private TMP_Text text;
    private Color startColor;
    private float timer;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        startColor = text.color;
    }

    public void SetText(string message, Color color)
    {
        text.text = message;
        text.color = color;
        startColor = color;
    }

    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
