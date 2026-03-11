using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFader : MonoBehaviour
{
    /// <summary>
    /// Fades the image from startAlpha to 0 over fadeTime seconds.
    /// </summary>
    /// <param name="image">The UI Image to fade.</param>
    /// <param name="startAlpha">The starting alpha (0-1).</param>
    /// <param name="fadeTime">Time in seconds to fade out.</param>
    public void FadeOut(Image image, float startAlpha, float fadeTime)
    {
        if (image == null) return;
        StartCoroutine(FadeRoutine(image, startAlpha, fadeTime));
    }

    private IEnumerator FadeRoutine(Image image, float startAlpha, float fadeTime)
    {
        Color color = image.color;
        color.a = startAlpha;
        image.color = color;

        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            image.color = color;
            yield return null;
        }

        // Ensure fully transparent at the end
        color.a = 0f;
        image.color = color;
    }
}