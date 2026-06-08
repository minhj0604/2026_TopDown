using UnityEngine;

public class GameSettingsManager : MonoBehaviour
{
    public const string MasterVolumeKey = "settings_master_volume";
    public const string FullscreenKey = "settings_fullscreen";
    public const string ResolutionIndexKey = "settings_resolution_index";
    public const string TutorialCompletedKey = "settings_tutorial_completed";

    [SerializeField] private float defaultMasterVolume = 1f;
    [SerializeField] private bool defaultFullscreen = true;

    public float MasterVolume { get; private set; }
    public bool IsFullscreen { get; private set; }
    public int ResolutionIndex { get; private set; }
    public bool TutorialCompleted { get; private set; }

    private void Awake()
    {
        LoadSettings();
        ApplySettings();
    }

    public void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        IsFullscreen = PlayerPrefs.GetInt(FullscreenKey, defaultFullscreen ? 1 : 0) == 1;
        ResolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
        TutorialCompleted = PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.SetInt(FullscreenKey, IsFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionIndexKey, ResolutionIndex);
        PlayerPrefs.SetInt(TutorialCompletedKey, TutorialCompleted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        ApplySettings();
        SaveSettings();
    }

    public void SetFullscreen(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        ApplySettings();
        SaveSettings();
    }

    public void SetResolutionIndex(int index)
    {
        ResolutionIndex = Mathf.Max(0, index);
        SaveSettings();
    }

    public void SetTutorialCompleted(bool completed)
    {
        TutorialCompleted = completed;
        SaveSettings();
    }

    private void ApplySettings()
    {
        AudioListener.volume = MasterVolume;
        Screen.fullScreen = IsFullscreen;
    }
}
