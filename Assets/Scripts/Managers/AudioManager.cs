using Unity.VisualScripting;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;


//For each sound make a name inside this Enum!!!!!!
public enum SFX
{
    Selling,
    JunkMerge,
    Objective,
    MergePotion,
    EnteredShop,
    Buying,
    SFX_Hover,
    SFX_Click,
    ShopOils,
    ShopGems,
    ShopHerbs,
    BookOpen,
    Coins
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }


    //Instance.AudioManager.Playsfx(volume, SFX.EnumName, pitch)   This is how you call it anywhere

    [Header("Settings")]
    [SerializeField] public AudioMixer audioMixer; //Unused yet
    [SerializeField] public AudioPool sfxPool;

    [Header("Music")]
    [SerializeField] private AudioMixerGroup musicOutputMixer;
    [SerializeField] private AudioClip openingMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField, Range(0f, 1f)] private float openingMusicVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 0.15f;
    [SerializeField, Min(0f)] private float musicCrossfadeDuration = 4.2f;

    private AudioSource openingMusicSource;
    private AudioSource gameplayMusicSource;
    private Coroutine musicTransition;


    //Add any sound you need here!!!!!!
    [Header("Sounds")]
    [Header("SFX")]
    [SerializeField] public AudioClip Selling;
    [SerializeField] public AudioClip Buying;
    [SerializeField] public AudioClip JunkMerge;
    [SerializeField] public AudioClip Objective;
    [SerializeField] public AudioClip MergePotion;
    [SerializeField] public AudioClip EnteredShop;
    [SerializeField] public AudioClip SFX_Hover;
    [SerializeField] public AudioClip SFX_Click;
    [SerializeField] public AudioClip ShopOils;
    [SerializeField] public AudioClip ShopGems;
    [SerializeField] public AudioClip ShopHerbs;
    [SerializeField] public AudioClip BookOpen;
    [SerializeField] public AudioClip Coins;


    public void Awake()
    {
        if (!Instance.IsUnityNull())
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        openingMusicSource = CreateMusicSource("Opening Music", openingMusic);
        gameplayMusicSource = CreateMusicSource("Gameplay Music", gameplayMusic);
    }

    public void PlayOpeningMusic()
    {
        StopMusicTransition();

        gameplayMusicSource.Stop();
        gameplayMusicSource.volume = 0f;

        openingMusicSource.clip = openingMusic;
        openingMusicSource.volume = openingMusicVolume;

        if (openingMusicSource.clip != null && !openingMusicSource.isPlaying)
            openingMusicSource.Play();
    }

    public void CrossfadeToGameplayMusic(float duration = -1f)
    {
        StopMusicTransition();

        float fadeDuration = duration >= 0f ? duration : musicCrossfadeDuration;
        musicTransition = StartCoroutine(CrossfadeToGameplayMusicRoutine(fadeDuration));
    }

    public void PlayGameplayMusicImmediately()
    {
        StopMusicTransition();

        openingMusicSource.Stop();
        openingMusicSource.volume = 0f;

        gameplayMusicSource.clip = gameplayMusic;
        gameplayMusicSource.volume = gameplayMusicVolume;

        if (gameplayMusicSource.clip != null && !gameplayMusicSource.isPlaying)
            gameplayMusicSource.Play();
    }

    public void FadeOutMusic(float duration)
    {
        StopMusicTransition();
        musicTransition = StartCoroutine(FadeOutMusicRoutine(Mathf.Max(0f, duration)));
    }

    private AudioSource CreateMusicSource(string sourceName, AudioClip clip)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.outputAudioMixerGroup = musicOutputMixer;
        return source;
    }

    private IEnumerator CrossfadeToGameplayMusicRoutine(float duration)
    {
        gameplayMusicSource.clip = gameplayMusic;
        gameplayMusicSource.volume = 0f;

        if (gameplayMusicSource.clip != null && !gameplayMusicSource.isPlaying)
            gameplayMusicSource.Play();

        float openingStartVolume = openingMusicSource.volume;

        if (duration <= 0f)
        {
            openingMusicSource.volume = 0f;
            gameplayMusicSource.volume = gameplayMusicVolume;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float smoothProgress = progress * progress * (3f - 2f * progress);

                openingMusicSource.volume = Mathf.Lerp(openingStartVolume, 0f, smoothProgress);
                gameplayMusicSource.volume = Mathf.Lerp(0f, gameplayMusicVolume, smoothProgress);
                yield return null;
            }
        }

        openingMusicSource.Stop();
        openingMusicSource.volume = 0f;
        gameplayMusicSource.volume = gameplayMusicVolume;
        musicTransition = null;
    }

    private IEnumerator FadeOutMusicRoutine(float duration)
    {
        float openingStartVolume = openingMusicSource.volume;
        float gameplayStartVolume = gameplayMusicSource.volume;

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float smoothProgress = progress * progress * (3f - 2f * progress);

                openingMusicSource.volume = Mathf.Lerp(openingStartVolume, 0f, smoothProgress);
                gameplayMusicSource.volume = Mathf.Lerp(gameplayStartVolume, 0f, smoothProgress);
                yield return null;
            }
        }

        openingMusicSource.Stop();
        gameplayMusicSource.Stop();
        openingMusicSource.volume = 0f;
        gameplayMusicSource.volume = 0f;
        musicTransition = null;
    }

    private void StopMusicTransition()
    {
        if (musicTransition == null)
            return;

        StopCoroutine(musicTransition);
        musicTransition = null;
    }

    private void PlaySfx(float volume, AudioClip audio, float pitch)
    {
        sfxPool.PlaySound(volume, audio, pitch);
    }

    public void PlaySfx(float volume, SFX sfx, float pitch)
    {

        //Here you make switch for each sfx you have added and it plays it that's it!
        switch (sfx)
        {
            case SFX.Selling:
                PlaySfx(volume, Selling, pitch);
                break;
            case SFX.JunkMerge:
                PlaySfx(volume, JunkMerge, pitch);
                break;
            case SFX.Objective:
                PlaySfx(volume, Objective, pitch);
                break;
            case SFX.MergePotion:
                PlaySfx(volume, MergePotion, pitch);
                break;
            case SFX.EnteredShop:
                PlaySfx(volume, EnteredShop, pitch);
                break;
            case SFX.Buying:
                PlaySfx(volume, Buying, pitch);
                break;
            case SFX.SFX_Hover:
                PlaySfx(volume, SFX_Hover, pitch);
                break;
            case SFX.SFX_Click:
                PlaySfx(volume, SFX_Click, pitch);
                break;
            case SFX.ShopOils:
                PlaySfx(volume, ShopOils, pitch);
                break;
            case SFX.ShopGems:
                PlaySfx(volume, ShopGems, pitch);
                break;
            case SFX.ShopHerbs:
                PlaySfx(volume, ShopHerbs, pitch);
                break;
            case SFX.BookOpen:
                PlaySfx(volume, BookOpen, pitch);
                break;
            case SFX.Coins:
                PlaySfx(volume, Coins, pitch);
                break;
            default:
                break;
        }
    }

    public float GetRandomPitch(float min, float max)
    {
        return Random.Range(min, max);
    }
}
