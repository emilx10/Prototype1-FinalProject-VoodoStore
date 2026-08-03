using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPool : MonoBehaviour
{
    [SerializeField] private int poolSize = 50;
    [SerializeField] private AudioMixerGroup outputMixer;

    private List<AudioSource> audioSources = new List<AudioSource>();

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        audioSources.RemoveAll(source => source == null);

        int sourcesToCreate = Mathf.Max(0, poolSize - audioSources.Count);
        for (int i = 0; i < sourcesToCreate; i++)
            AddSource();
    }

    private AudioSource AddSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = outputMixer;
        source.playOnAwake = false;
        audioSources.Add(source);
        return source;
    }

    private void Start()
    {
        // Awake normally fills the pool. This also repairs it after a domain reload.
        EnsureInitialized();
    }

    public void PlaySound(float volume, AudioClip audio, float pitch)
    {
        if (audio == null)
            return;

        EnsureInitialized();

        AudioSource availableSource = audioSources.Find(source => source != null && !source.isPlaying);

        if (availableSource == null)
            availableSource = AddSource();

        availableSource.pitch = pitch;
        availableSource.volume = volume;
        availableSource.clip = audio;
        availableSource.Play();
    }

}
