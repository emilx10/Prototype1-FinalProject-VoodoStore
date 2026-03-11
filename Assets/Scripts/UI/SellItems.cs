using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SellItems : MonoBehaviour
{
    [Header("Effect Settings")]
    public Canvas canvas;
    public Image itemImagePrefab;
    public int spawnCount = 5;           // Default for cheap items
    public float radius = 50f;
    public float flyDuration = 0.5f;
    public float delayBetween = 0.05f;
    public float startScale = 1f;
    public float endScale = 0.5f;

    public Image myImage;
    public UIFader fader;

    [Header("Transforms")]
    public Transform spawnTransform;
    public Transform targetTransform;

    [Header("Coin Settings")]
    public int cheapCoins = 5;
    public int expensiveCoins = 20;

    private void OnEnable()
    {
        GameManager.OnItemSold += SellItemSell;
        GameManager.OnSuccessfulMerge += SuccessfullItem;
        GameManager.OnFailedMerge += badCraft;
        GameManager.OnItemBought += TakeCoinsEffect;
    }

    private void OnDisable()
    {
        GameManager.OnItemSold -= SellItemSell;
        GameManager.OnSuccessfulMerge -= SuccessfullItem;
        GameManager.OnFailedMerge -= badCraft;
        GameManager.OnItemBought -= TakeCoinsEffect;
    }

    public void SuccessfullItem()
    {
        Debug.Log("EVENT: OnSuccessfulMerge triggered");

        fader.FadeOut(myImage, 0.3f, 1f, UIFader.FadeColor.Green);
    }

    public void badCraft()
    {
        Debug.Log("EVENT: badCraft triggered");
        fader.FadeOut(myImage, 0.3f, 1f, UIFader.FadeColor.Purple);
    }

    /// <summary>
    /// Call this when an item is sold
    /// </summary>
    /// <param name="isCheap">Whether the item is cheap</param>
    public void SellItemSell(bool isCheap)
    {
        // Fade some UI image (like a coin indicator)
        fader.FadeOut(myImage, 0.13f, 1f, UIFader.FadeColor.Yellow);

        if (itemImagePrefab != null && spawnTransform != null && targetTransform != null && canvas != null)
        {
            // Adjust spawn count based on cheap or expensive
            int imagesToSpawn = isCheap ? spawnCount : spawnCount * 2; // Expensive spawns more
            StartCoroutine(SpawnAndFlyMultiple(itemImagePrefab, spawnTransform.position, imagesToSpawn, isCheap));
        }
    }

    public void TakeCoinsEffect()
    {
        if (itemImagePrefab != null && spawnTransform != null && targetTransform != null && canvas != null)
        {
            StartCoroutine(SpawnAndFlyBack(itemImagePrefab, targetTransform.position, 10));
        }
    }

    private IEnumerator SpawnAndFlyBack(Image prefab, Vector3 startPos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;

            Image img = Instantiate(prefab, canvas.transform);
            img.transform.position = startPos;
            img.transform.localScale = Vector3.one * endScale;

            Vector3 randomEnd = spawnTransform.position + (Vector3)offset;

            StartCoroutine(FlyBack(img, randomEnd));

            yield return new WaitForSeconds(delayBetween);
        }
    }

    private IEnumerator FlyBack(Image img, Vector3 endPos)
    {
        float elapsed = 0f;

        Vector3 startPos = img.transform.position;
        Vector3 startScaleVector = Vector3.one * endScale;
        Vector3 endScaleVector = Vector3.one * startScale;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            img.transform.position = Vector3.Lerp(startPos, endPos, t);
            img.transform.localScale = Vector3.Lerp(startScaleVector, endScaleVector, t);

            yield return null;
        }

        img.transform.position = endPos;
        img.transform.localScale = endScaleVector;

        Destroy(img.gameObject);
    }

    private IEnumerator SpawnAndFlyMultiple(Image prefab, Vector3 spawnPos, int imagesToSpawn, bool isCheap)
    {
        for (int i = 0; i < imagesToSpawn; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Image img = Instantiate(prefab, canvas.transform);
            img.transform.position = spawnPos + (Vector3)offset;
            img.transform.localScale = Vector3.one * startScale;

            // Each flying coin plays sound and adds coins at the end
            StartCoroutine(FlyToTarget(img, targetTransform.position, isCheap));

            yield return new WaitForSeconds(delayBetween);
        }
    }

    private IEnumerator FlyToTarget(Image img, Vector3 targetPos, bool isCheap)
    {
        float elapsed = 0f;
        Vector3 startPos = img.transform.position;
        Vector3 startScaleVector = Vector3.one * startScale;
        Vector3 endScaleVector = Vector3.one * endScale;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            img.transform.position = Vector3.Lerp(startPos, targetPos, t);
            img.transform.localScale = Vector3.Lerp(startScaleVector, endScaleVector, t);

            yield return null;
        }

        img.transform.position = targetPos;
        img.transform.localScale = endScaleVector;

        // Play coin sound
        AudioManager.Instance.PlaySfx(0.1f, SFX.Buying, 1.1f);

        Destroy(img.gameObject);
    }
}