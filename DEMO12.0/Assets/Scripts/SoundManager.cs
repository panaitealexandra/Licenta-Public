using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Sound Effects")]
    public AudioClip playerShootSound;
    public AudioClip enemyShootSound;
    public AudioClip playerHitSound;
    public AudioClip enemyHitSound;
    public AudioClip bossHitSound;

    [Header("Audio Sources")]
    public AudioSource effectsSource;
    public AudioSource shootSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float effectsVolume = 0.7f;
    [Range(0f, 1f)]
    public float shootVolume = 0.5f;

    private const string EFFECTS_VOLUME_KEY = "EffectsVolume";
    private const string EFFECTS_ENABLED_KEY = "EffectsEnabled";
    private bool effectsEnabled = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (effectsSource == null)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
        }

        if (shootSource == null)
        {
            shootSource = gameObject.AddComponent<AudioSource>();
            shootSource.playOnAwake = false;
        }

        LoadPreferences();
    }

    void LoadPreferences()
    {
        effectsEnabled = PlayerPrefs.GetInt(EFFECTS_ENABLED_KEY, 1) == 1;
        effectsVolume = PlayerPrefs.GetFloat(EFFECTS_VOLUME_KEY, 0.7f);

        UpdateVolumes();
    }

    void SavePreferences()
    {
        PlayerPrefs.SetInt(EFFECTS_ENABLED_KEY, effectsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(EFFECTS_VOLUME_KEY, effectsVolume);
        PlayerPrefs.Save();
    }

    void UpdateVolumes()
    {
        if (effectsSource != null)
            effectsSource.volume = effectsEnabled ? effectsVolume : 0f;

        if (shootSource != null)
            shootSource.volume = effectsEnabled ? shootVolume : 0f;
    }

    public void PlayPlayerShoot()
    {
        if (playerShootSound != null && effectsEnabled)
        {
            shootSource.PlayOneShot(playerShootSound, shootVolume);
        }
    }

    public void PlayEnemyShoot()
    {
        if (enemyShootSound != null && effectsEnabled)
        {
            shootSource.PlayOneShot(enemyShootSound, shootVolume * 0.7f); 
        }
    }

    public void PlayPlayerHit()
    {
        if (playerHitSound != null && effectsEnabled)
        {
            effectsSource.PlayOneShot(playerHitSound, effectsVolume);
        }
    }

    public void PlayEnemyHit()
    {
        if (enemyHitSound != null && effectsEnabled)
        {
            effectsSource.PlayOneShot(enemyHitSound, effectsVolume * 0.8f);
        }
    }

    public void PlayBossHit()
    {
        if (bossHitSound != null && effectsEnabled)
        {
            effectsSource.PlayOneShot(bossHitSound, effectsVolume);
        }
    }

    public void PlaySound(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && effectsEnabled)
        {
            effectsSource.PlayOneShot(clip, effectsVolume * volumeMultiplier);
        }
    }

    public void SetEffectsEnabled(bool enabled)
    {
        effectsEnabled = enabled;
        UpdateVolumes();
        SavePreferences();
    }

    public void SetEffectsVolume(float volume)
    {
        effectsVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SavePreferences();
    }

    public bool AreEffectsEnabled()
    {
        return effectsEnabled;
    }
}