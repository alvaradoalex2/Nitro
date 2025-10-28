using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
    [HideInInspector] public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    [SerializeField] private Sound[] sounds;

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.5f;

    private Dictionary<string, Sound> soundDictionary;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudio();
    }

    private void InitializeAudio()
    {
        soundDictionary = new Dictionary<string, Sound>();

        // Setup sound effects
        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;

            soundDictionary[sound.name] = sound;
        }

        // Setup background music
        if (backgroundMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlaySound(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (sound.source != null)
            {
                sound.source.Play();
            }
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
        }
    }

    public void StopSound(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (sound.source != null)
            {
                sound.source.Stop();
            }
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        foreach (var sound in sounds)
        {
            if (sound.source != null)
            {
                sound.source.volume = sound.volume * volume;
            }
        }
    }

    public void ToggleMusic(bool enabled)
    {
        if (musicSource != null)
        {
            if (enabled && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
            else if (!enabled && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}