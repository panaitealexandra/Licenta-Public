using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private bool isMusicEnabled = true;
    private const string MUSIC_PREF_KEY = "BackgroundMusicEnabled";
    private const string VOLUME_PREF_KEY = "MusicVolume";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); 

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        LoadPreferences();

        if (backgroundMusic != null && isMusicEnabled)
        {
            PlayMusic();
        }
    }

    void LoadPreferences()
    {
        isMusicEnabled = PlayerPrefs.GetInt(MUSIC_PREF_KEY, 1) == 1;
        musicVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 0.5f);

        if (musicSource != null)
        {
            musicSource.volume = isMusicEnabled ? musicVolume : 0f;
        }
    }

    void SavePreferences()
    {
        PlayerPrefs.SetInt(MUSIC_PREF_KEY, isMusicEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, musicVolume);
        PlayerPrefs.Save();
    }

    public void PlayMusic()
    {
        if (backgroundMusic == null) return;

        musicSource.clip = backgroundMusic;
        musicSource.volume = isMusicEnabled ? musicVolume : 0f;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;

        if (isMusicEnabled)
        {
            musicSource.volume = musicVolume;
            if (!musicSource.isPlaying && musicSource.clip != null)
            {
                musicSource.Play();
            }
        }
        else
        {
            musicSource.volume = 0f;
        }

        SavePreferences();
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (isMusicEnabled)
        {
            musicSource.volume = musicVolume;
        }
        SavePreferences();
    }

    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }
}