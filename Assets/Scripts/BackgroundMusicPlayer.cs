using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    public static BackgroundMusicPlayer Instance { get; private set; }

    private const string MusicVolumeKey = "MusicVolume";
    private const string MusicEnabledKey = "MusicEnabled";

    [SerializeField] private AudioClip defaultTrack;
    [Range(0f, 1f)][SerializeField] private float defaultVolume = 0.5f;
    [SerializeField] private bool playOnStart = true;

    private AudioSource audioSource;
    private bool isMusicEnabled = true;

    public float Volume => audioSource != null ? audioSource.volume : 0f;
    public bool IsMusicEnabled => isMusicEnabled;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
        LoadPersistedSettings();

        if (playOnStart)
        {
            Play(defaultTrack);
        }
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = defaultVolume;
    }

    private void LoadPersistedSettings()
    {
        float storedVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultVolume);
        bool storedEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;

        SetVolume(storedVolume, false);
        SetMusicEnabled(storedEnabled, false);
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            clip = defaultTrack;
        }

        if (clip == null)
        {
            return;
        }

        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.clip = clip;

        if (isMusicEnabled)
        {
            audioSource.Play();
        }
    }

    public void Stop()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void SetVolume(float value, bool save = true)
    {
        value = Mathf.Clamp01(value);

        if (audioSource != null)
        {
            audioSource.volume = value;
        }

        if (save)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            PlayerPrefs.Save();
        }
    }

    public void SetMusicEnabled(bool enabled, bool save = true)
    {
        isMusicEnabled = enabled;

        if (audioSource != null)
        {
            audioSource.mute = !enabled;

            if (enabled && !audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        if (save)
        {
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public void ToggleMusic()
    {
        SetMusicEnabled(!isMusicEnabled);
    }
}
