using System.Collections.Generic;
using UnityEngine;

public class CauldronAliveEffect : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Vector3 localCenter = new Vector3(0f, 5.35f, 0f);
    [SerializeField] private Vector2 liquidSize = new Vector2(3.2f, 0.62f);
    [SerializeField] private int sortingOrder = 98;

    [Header("Motion")]
    [SerializeField] private float swirlSpeed = 32f;
    [SerializeField] private float pulseAmount = 0.055f;
    [SerializeField] private float pulseSpeed = 2.6f;
    [SerializeField] private float bubbleRiseHeight = 0.62f;
    [SerializeField] private float bubbleLifetime = 1.45f;

    [Header("Look")]
    [SerializeField] private Color liquidColor = new Color(0.16f, 0.92f, 0.48f, 0.82f);
    [SerializeField] private Color glowColor = new Color(0.38f, 1f, 0.68f, 0.46f);
    [SerializeField] private Color bubbleColor = new Color(0.78f, 1f, 0.86f, 0.72f);

    private const int BubbleCount = 11;

    private readonly List<Bubble> bubbles = new List<Bubble>();
    private Transform effectRoot;
    private Transform liquid;
    private Transform swirlA;
    private Transform swirlB;
    private Sprite softCircleSprite;
    private Sprite ringSprite;
    private float seed;

    private struct Bubble
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 start;
        public float delay;
        public float size;
        public float drift;
    }

    private void OnEnable()
    {
        seed = Random.value * 10f;
        BuildEffect();
    }

    private void OnDisable()
    {
        if (effectRoot == null) return;

        if (Application.isPlaying)
            Destroy(effectRoot.gameObject);
        else
            DestroyImmediate(effectRoot.gameObject);

        effectRoot = null;
        bubbles.Clear();
    }

    private void Update()
    {
        if (effectRoot == null) return;

        float time = Time.time + seed;
        float pulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;

        liquid.localScale = new Vector3(liquidSize.x * pulse, liquidSize.y * (1f - pulseAmount * 0.45f), 1f);
        swirlA.Rotate(0f, 0f, swirlSpeed * Time.deltaTime);
        swirlB.Rotate(0f, 0f, -swirlSpeed * 0.62f * Time.deltaTime);

        for (int i = 0; i < bubbles.Count; i++)
        {
            Bubble bubble = bubbles[i];
            float t = Mathf.Repeat((time + bubble.delay) / bubbleLifetime, 1f);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            float sideDrift = Mathf.Sin((time + bubble.delay) * 4.2f) * bubble.drift;
            float alpha = Mathf.Sin(t * Mathf.PI) * bubbleColor.a;

            bubble.transform.localPosition = new Vector3(
                bubble.start.x + sideDrift,
                bubble.start.y + eased * bubbleRiseHeight,
                -0.01f);

            float scale = bubble.size * (0.55f + t * 0.85f);
            bubble.transform.localScale = new Vector3(scale, scale, 1f);
            bubble.renderer.color = new Color(bubbleColor.r, bubbleColor.g, bubbleColor.b, alpha);
        }
    }

    private void BuildEffect()
    {
        if (effectRoot != null) return;

        softCircleSprite = CreateSoftCircleSprite("Cauldron Potion Soft Circle", false);
        ringSprite = CreateSoftCircleSprite("Cauldron Potion Ring", true);

        effectRoot = new GameObject("Cauldron Alive Effect").transform;
        effectRoot.SetParent(transform, false);
        effectRoot.localPosition = localCenter;
        effectRoot.localRotation = Quaternion.identity;
        effectRoot.localScale = Vector3.one;

        liquid = CreateLayer("Potion Surface", softCircleSprite, liquidColor, sortingOrder, liquidSize, 0f);
        swirlA = CreateLayer("Potion Swirl A", ringSprite, glowColor, sortingOrder + 1, liquidSize * 0.72f, 18f);
        swirlB = CreateLayer("Potion Swirl B", ringSprite, glowColor * new Color(1f, 1f, 1f, 0.72f), sortingOrder + 1, liquidSize * 0.48f, -34f);

        for (int i = 0; i < BubbleCount; i++)
        {
            float x = Mathf.Lerp(-liquidSize.x * 0.38f, liquidSize.x * 0.38f, (i + 0.5f) / BubbleCount);
            x += Random.Range(-0.18f, 0.18f);

            var bubbleTransform = new GameObject("Potion Bubble").transform;
            bubbleTransform.SetParent(effectRoot, false);
            bubbleTransform.localPosition = new Vector3(x, Random.Range(-0.18f, 0.12f), -0.01f);

            SpriteRenderer renderer = bubbleTransform.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ringSprite;
            renderer.color = bubbleColor;
            renderer.sortingOrder = sortingOrder + 2;

            bubbles.Add(new Bubble
            {
                transform = bubbleTransform,
                renderer = renderer,
                start = bubbleTransform.localPosition,
                delay = Random.Range(0f, bubbleLifetime),
                size = Random.Range(0.075f, 0.15f),
                drift = Random.Range(0.025f, 0.08f)
            });
        }
    }

    private Transform CreateLayer(string layerName, Sprite sprite, Color color, int order, Vector2 scale, float zRotation)
    {
        Transform layer = new GameObject(layerName).transform;
        layer.SetParent(effectRoot, false);
        layer.localPosition = Vector3.zero;
        layer.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        layer.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer renderer = layer.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;

        return layer;
    }

    private static Sprite CreateSoftCircleSprite(string spriteName, bool ring)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = ring
                    ? Mathf.SmoothStep(0.12f, 0.95f, distance) * (1f - Mathf.SmoothStep(0.82f, 1f, distance))
                    : 1f - Mathf.SmoothStep(0.42f, 1f, distance);

                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
