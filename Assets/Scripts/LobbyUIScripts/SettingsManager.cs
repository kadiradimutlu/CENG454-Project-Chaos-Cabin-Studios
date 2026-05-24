using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public const float DefaultMouseSensitivity = 1f;
    public const float MinMouseSensitivity = 0.2f;
    public const float MaxMouseSensitivity = 3f;

    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const string QualityLevelKey = "QualityLevel";
    private const string FullscreenKey = "Fullscreen";
    private const string ResolutionIndexKey = "ResolutionIndex";
    private const string MouseSensitivityKey = "MouseSensitivity";

    private static float mouseSensitivity = DefaultMouseSensitivity;
    private static bool mouseSensitivityLoaded;

    public static float MouseSensitivity
    {
        get
        {
            EnsureMouseSensitivityLoaded();
            return mouseSensitivity;
        }
        private set
        {
            mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
            mouseSensitivityLoaded = true;
        }
    }

    public static event Action<float> MouseSensitivityChanged;

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Display & Graphics Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Camera Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI mouseSensitivityValueText;

    private Resolution[] _availableResolutions;
    private bool _isInitializing = true;

    private static void EnsureMouseSensitivityLoaded()
    {
        if (mouseSensitivityLoaded)
            return;

        mouseSensitivity = Mathf.Clamp(
            PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity),
            MinMouseSensitivity,
            MaxMouseSensitivity);

        mouseSensitivityLoaded = true;
    }

    private void OnEnable()
    {
        _isInitializing = true;
        InitializeResolutions();
        LoadSettings();
        _isInitializing = false;
    }

    private void InitializeResolutions()
    {
        _availableResolutions = Screen.resolutions;

        if (resolutionDropdown == null)
            return;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < _availableResolutions.Length; i++)
        {
            string option = _availableResolutions[i].width + " x " + _availableResolutions[i].height;
            options.Add(option);

            if (_availableResolutions[i].width == Screen.currentResolution.width &&
                _availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetMasterVolume(float volume)
    {
        if (_isInitializing)
            return;

        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", volume);

        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        if (_isInitializing)
            return;

        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", volume);

        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (_isInitializing)
            return;

        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", volume);

        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetQuality(int qualityIndex)
    {
        if (_isInitializing)
            return;

        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(QualityLevelKey, qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (_isInitializing)
            return;

        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (_isInitializing)
            return;

        if (_availableResolutions == null ||
            resolutionIndex < 0 ||
            resolutionIndex >= _availableResolutions.Length)
        {
            return;
        }

        Resolution resolution = _availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    public void SetMouseSensitivity(float value)
    {
        if (_isInitializing)
            return;

        ApplyMouseSensitivity(value, true, true);
    }

    private void LoadSettings()
    {
        LoadAudioSettings();
        LoadGraphicsSettings();
        LoadCameraSettings();
    }

    private void LoadAudioSettings()
    {
        if (PlayerPrefs.HasKey(MasterVolumeKey))
        {
            float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey);

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(savedVolume);

            if (audioMixer != null)
                audioMixer.SetFloat("MasterVolume", savedVolume);
        }

        if (PlayerPrefs.HasKey(MusicVolumeKey))
        {
            float savedVolume = PlayerPrefs.GetFloat(MusicVolumeKey);

            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(savedVolume);

            if (audioMixer != null)
                audioMixer.SetFloat("MusicVolume", savedVolume);
        }

        if (PlayerPrefs.HasKey(SfxVolumeKey))
        {
            float savedVolume = PlayerPrefs.GetFloat(SfxVolumeKey);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(savedVolume);

            if (audioMixer != null)
                audioMixer.SetFloat("SFXVolume", savedVolume);
        }
    }

    private void LoadGraphicsSettings()
    {
        if (PlayerPrefs.HasKey(QualityLevelKey))
        {
            int savedQuality = PlayerPrefs.GetInt(QualityLevelKey);

            if (qualityDropdown != null)
                qualityDropdown.SetValueWithoutNotify(savedQuality);

            QualitySettings.SetQualityLevel(savedQuality);
        }

        if (PlayerPrefs.HasKey(FullscreenKey))
        {
            bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey) == 1;

            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);

            Screen.fullScreen = isFullscreen;
        }

        if (PlayerPrefs.HasKey(ResolutionIndexKey) && _availableResolutions != null)
        {
            int savedResolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey);

            if (savedResolutionIndex >= 0 && savedResolutionIndex < _availableResolutions.Length)
            {
                if (resolutionDropdown != null)
                    resolutionDropdown.SetValueWithoutNotify(savedResolutionIndex);

                Resolution resolution = _availableResolutions[savedResolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            }
        }
    }

    private void LoadCameraSettings()
    {
        float savedSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
        ApplyMouseSensitivity(savedSensitivity, false, false);
    }

    private void ApplyMouseSensitivity(float value, bool save, bool notify)
    {
        MouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = MinMouseSensitivity;
            mouseSensitivitySlider.maxValue = MaxMouseSensitivity;
            mouseSensitivitySlider.SetValueWithoutNotify(MouseSensitivity);
        }

        UpdateMouseSensitivityText();

        if (save)
        {
            PlayerPrefs.SetFloat(MouseSensitivityKey, MouseSensitivity);
            PlayerPrefs.Save();
        }

        if (notify)
            MouseSensitivityChanged?.Invoke(MouseSensitivity);
    }

    private void UpdateMouseSensitivityText()
    {
        if (mouseSensitivityValueText != null)
            mouseSensitivityValueText.text = MouseSensitivity.ToString("0.0");
    }
}
