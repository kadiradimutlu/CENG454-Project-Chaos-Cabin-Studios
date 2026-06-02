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

    private const float MinMixerVolume = -80f;
    private const float MaxMixerVolume = 0f;

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

    public static void ApplySavedAudioSettings(AudioMixer targetMixer)
    {
        if (targetMixer == null)
            return;

        ApplySavedVolumeToMixer(targetMixer, MasterVolumeKey, "MasterVolume", 1f);
        ApplySavedVolumeToMixer(targetMixer, MusicVolumeKey, "MusicVolume", 1f);
        ApplySavedVolumeToMixer(targetMixer, SfxVolumeKey, "SFXVolume", 1f);
    }

    private static void ApplySavedVolumeToMixer(AudioMixer targetMixer, string prefsKey, string mixerParameter, float defaultValue)
    {
        float volume = LoadVolumeValue(prefsKey, defaultValue);
        float mixerValue = SliderValueToDb(volume);
        targetMixer.SetFloat(mixerParameter, mixerValue);
    }

    private static void ApplyMainMenuMusicVolume(float volume)
    {
        MainMenuManager mainMenuManager = FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);

        if (mainMenuManager != null)
            mainMenuManager.SetMainMenuMusicVolume(volume);
    }


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


    private void Awake()
    {
        ApplySavedAudioSettings(audioMixer);
    }

    private void OnEnable()
    {
        _isInitializing = true;
        InitializeAudioSliders();
        InitializeResolutions();
        LoadSettings();
        _isInitializing = false;
    }

    private void InitializeAudioSliders()
    {
        SetupVolumeSlider(masterVolumeSlider);
        SetupVolumeSlider(musicVolumeSlider);
        SetupVolumeSlider(sfxVolumeSlider);
    }

    private void SetupVolumeSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
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

        ApplyVolume(MasterVolumeKey, "MasterVolume", volume, true);
    }

    public void SetMusicVolume(float volume)
    {
        if (_isInitializing)
            return;

        ApplyVolume(MusicVolumeKey, "MusicVolume", volume, true);
        ApplyMainMenuMusicVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (_isInitializing)
            return;

        ApplyVolume(SfxVolumeKey, "SFXVolume", volume, true);
    }

    public void SetQuality(int qualityIndex)
    {
        if (_isInitializing)
            return;

        int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
        int safeQualityIndex = Mathf.Clamp(qualityIndex, 0, maxQualityIndex);

        QualitySettings.SetQualityLevel(safeQualityIndex, true);
        PlayerPrefs.SetInt(QualityLevelKey, safeQualityIndex);
        PlayerPrefs.Save();

        if (qualityDropdown != null && qualityDropdown.value != safeQualityIndex)
            qualityDropdown.SetValueWithoutNotify(safeQualityIndex);
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
        float masterVolume = LoadVolumeValue(MasterVolumeKey, 1f);
        float musicVolume = LoadVolumeValue(MusicVolumeKey, 1f);
        float sfxVolume = LoadVolumeValue(SfxVolumeKey, 1f);

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);

        ApplyVolume(MasterVolumeKey, "MasterVolume", masterVolume, false);
        ApplyVolume(MusicVolumeKey, "MusicVolume", musicVolume, false);
        ApplyVolume(SfxVolumeKey, "SFXVolume", sfxVolume, false);
        ApplyMainMenuMusicVolume(musicVolume);
    }

    private static float LoadVolumeValue(string key, float defaultValue)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        float savedValue = PlayerPrefs.GetFloat(key, defaultValue);

        if (savedValue < 0f)
            return DbToSliderValue(savedValue);

        return Mathf.Clamp01(savedValue);
    }

    private void ApplyVolume(string prefsKey, string mixerParameter, float sliderValue, bool save)
    {
        float normalizedValue = Mathf.Clamp01(sliderValue);
        float mixerValue = SliderValueToDb(normalizedValue);

        if (audioMixer != null)
            audioMixer.SetFloat(mixerParameter, mixerValue);

        if (save)
        {
            PlayerPrefs.SetFloat(prefsKey, normalizedValue);
            PlayerPrefs.Save();
        }
    }

    private static float SliderValueToDb(float value)
    {
        if (value <= 0.0001f)
            return MinMixerVolume;

        return Mathf.Clamp(Mathf.Log10(value) * 20f, MinMixerVolume, MaxMixerVolume);
    }

    private static float DbToSliderValue(float dbValue)
    {
        if (dbValue <= MinMixerVolume)
            return 0f;

        return Mathf.Clamp01(Mathf.Pow(10f, dbValue / 20f));
    }

    private void LoadGraphicsSettings()
    {
        if (PlayerPrefs.HasKey(QualityLevelKey))
        {
            int savedQuality = PlayerPrefs.GetInt(QualityLevelKey);
            int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
            int safeQualityIndex = Mathf.Clamp(savedQuality, 0, maxQualityIndex);

            if (qualityDropdown != null)
                qualityDropdown.SetValueWithoutNotify(safeQualityIndex);

            QualitySettings.SetQualityLevel(safeQualityIndex, true);
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
