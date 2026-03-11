using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFader : MonoBehaviour
{
    public enum FadeColor
    {
        Yellow,
        Purple,
        Green
    }

    public void FadeOut(Image image, float startAlpha, float fadeTime, FadeColor fadeColor)
    {
        if (image == null) return;

        Color color = GetColorFromHex(fadeColor);
        color.a = startAlpha;
        image.color = color;

        StartCoroutine(FadeRoutine(image, startAlpha, fadeTime));
    }

    private IEnumerator FadeRoutine(Image image, float startAlpha, float fadeTime)
    {
        Color color = image.color;

        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }

    private Color GetColorFromHex(FadeColor fadeColor)
    {
        string hex = "#FFFFFF";

        switch (fadeColor)
        {
            case FadeColor.Yellow:
                hex = "#FFD26D";
                break;

            case FadeColor.Green:
                hex = "#3BFF6A";
                break;

            case FadeColor.Purple:
                hex = "#D26EFF";
                break;
        }

        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}