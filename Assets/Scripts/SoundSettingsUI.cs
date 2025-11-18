using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Toggle musicToggle;

    private void OnEnable()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(HandleToggleChanged);
        }

        SyncUI();
    }

    private void OnDisable()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }
    }

    private void SyncUI()
    {
        var player = BackgroundMusicPlayer.Instance;
        if (player == null)
        {
            return;
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(player.Volume);
        }

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(player.IsMusicEnabled);
        }
    }

    private void HandleVolumeChanged(float value)
    {
        BackgroundMusicPlayer.Instance?.SetVolume(value);
    }

    private void HandleToggleChanged(bool enabled)
    {
        BackgroundMusicPlayer.Instance?.SetMusicEnabled(enabled);
    }
}
