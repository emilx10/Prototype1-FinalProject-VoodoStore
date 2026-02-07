using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;


//For each sound make a name inside this Enum!!!!!!
public enum SFX
{
    Test,
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
    [SerializeField] public AudioClip anySound;
    

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
            case SFX.Test:
                PlaySfx(volume, anySound, pitch);
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
