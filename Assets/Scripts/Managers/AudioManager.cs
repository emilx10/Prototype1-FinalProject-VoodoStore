using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;


//For each sound make a name inside this Enum!!!!!!
public enum SFX
{
    None,
    Selling,
    JunkMerge,
    Objective,
    MergePotion,
    EnteredShop,
    Buying,
    UI_Button_Hover,
    GemMarketSelected,
    OilMarketSelected,
    HerbMarketSelected
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
    [SerializeField] public AudioClip UiHover;
    [SerializeField] public AudioClip GemMarketSFX;
    [SerializeField] public AudioClip OilMarketSFX;
    [SerializeField] public AudioClip HerbMarketSFX;
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
            case SFX.UI_Button_Hover:
                PlaySfx(volume, UiHover, pitch);
                break;
            case SFX.GemMarketSelected:
                PlaySfx(volume, GemMarketSFX, pitch);
                break;
            case SFX.HerbMarketSelected:
                PlaySfx(volume, HerbMarketSFX, pitch);
                break;
            case SFX.OilMarketSelected:
                PlaySfx(volume, OilMarketSFX, pitch);
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
