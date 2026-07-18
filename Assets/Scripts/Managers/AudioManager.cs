using Unity.VisualScripting;
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
