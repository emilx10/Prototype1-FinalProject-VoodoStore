using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SpriteRendererToUIImageConverter
{
    private const string MenuPath = "Tools/UI/Convert Selected SpriteRenderers To UI Images";

    [MenuItem(MenuPath, true)]
    private static bool CanConvertSelected()
    {
        return Selection.activeTransform != null &&
            Selection.activeTransform.GetComponentInParent<Canvas>() != null &&
            Selection.activeTransform.GetComponentsInChildren<SpriteRenderer>(true).Length > 0;
    }

    [MenuItem(MenuPath)]
    private static void ConvertSelected()
    {
        Transform sourceRoot = Selection.activeTransform;
        Canvas canvas = sourceRoot.GetComponentInParent<Canvas>();

        if (sourceRoot == null || canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Convert SpriteRenderers",
                "Select a SpriteRenderer hierarchy that is already under a Canvas.",
                "OK");
            return;
        }

        GameObject uiRootObject = new GameObject($"{sourceRoot.name} UI", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(uiRootObject, "Convert SpriteRenderers To UI Images");

        Transform parent = sourceRoot.parent != null ? sourceRoot.parent : canvas.transform;
        uiRootObject.transform.SetParent(parent, false);
        uiRootObject.transform.SetSiblingIndex(sourceRoot.GetSiblingIndex() + 1);

        RectTransform uiRoot = uiRootObject.GetComponent<RectTransform>();
        CopyRectTransform(sourceRoot, uiRoot);

        ConvertChildren(sourceRoot, uiRoot, sourceRoot, uiRoot);

        bool disableOriginal = EditorUtility.DisplayDialog(
            "Convert SpriteRenderers",
            "Created a UI Image copy. Disable the original SpriteRenderer hierarchy now?",
            "Disable Original",
            "Keep Original");

        if (disableOriginal)
        {
            Undo.RecordObject(sourceRoot.gameObject, "Disable Original SpriteRenderer Hierarchy");
            sourceRoot.gameObject.SetActive(false);
        }

        Selection.activeGameObject = uiRootObject;
        EditorUtility.SetDirty(uiRootObject);
    }

    private static void ConvertChildren(
        Transform source,
        RectTransform targetParent,
        Transform sourceRoot,
        RectTransform uiRoot)
    {
        SpriteRenderer spriteRenderer = source.GetComponent<SpriteRenderer>();
        RectTransform currentTarget = targetParent;

        if (spriteRenderer != null)
        {
            GameObject imageObject = new GameObject(source.name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(imageObject, "Create UI Image Layer");
            imageObject.transform.SetParent(targetParent, false);
            imageObject.transform.SetSiblingIndex(source.GetSiblingIndex());

            currentTarget = imageObject.GetComponent<RectTransform>();
            CopySpriteLayerToImage(source, sourceRoot, currentTarget, uiRoot, spriteRenderer);
        }

        for (int i = 0; i < source.childCount; i++)
            ConvertChildren(source.GetChild(i), currentTarget, sourceRoot, uiRoot);
    }

    private static void CopySpriteLayerToImage(
        Transform source,
        Transform sourceRoot,
        RectTransform target,
        RectTransform uiRoot,
        SpriteRenderer spriteRenderer)
    {
        Image image = target.GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;
        image.color = spriteRenderer.color;
        image.preserveAspect = false;
        image.raycastTarget = false;

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);

        Vector3 localPoint = sourceRoot.InverseTransformPoint(source.position);
        float pixelsPerUnit = spriteRenderer.sprite != null ? spriteRenderer.sprite.pixelsPerUnit : 100f;
        target.anchoredPosition = new Vector2(localPoint.x * pixelsPerUnit, localPoint.y * pixelsPerUnit);

        if (spriteRenderer.sprite != null)
            target.sizeDelta = spriteRenderer.sprite.rect.size;

        target.localRotation = source.localRotation;
        target.localScale = new Vector3(
            Mathf.Abs(source.lossyScale.x / Mathf.Max(0.0001f, sourceRoot.lossyScale.x)),
            Mathf.Abs(source.lossyScale.y / Mathf.Max(0.0001f, sourceRoot.lossyScale.y)),
            1f);

        if (spriteRenderer.flipX)
            target.localScale = new Vector3(-target.localScale.x, target.localScale.y, target.localScale.z);

        if (spriteRenderer.flipY)
            target.localScale = new Vector3(target.localScale.x, -target.localScale.y, target.localScale.z);
    }

    private static void CopyRectTransform(Transform source, RectTransform target)
    {
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = new Vector2(source.localPosition.x, source.localPosition.y);
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
        target.sizeDelta = Vector2.zero;
    }
}
