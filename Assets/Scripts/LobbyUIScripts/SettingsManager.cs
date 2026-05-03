using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Display & Graphics Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private Resolution[] _availableResolutions;
    private bool _isInitializing = true;

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
        if (resolutionDropdown != null)
        {
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
    }

    public void SetMasterVolume(float volume)
    {
        if (_isInitializing) return; 
        audioMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        if (_isInitializing) return; 
        audioMixer.SetFloat("MusicVolume", volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (_isInitializing) return; 
        audioMixer.SetFloat("SFXVolume", volume);
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetQuality(int qualityIndex)
    {
        if (_isInitializing) return;
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (_isInitializing) return;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (_isInitializing) return;
        Resolution resolution = _availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume");
            if (masterVolumeSlider != null) masterVolumeSlider.value = savedVolume;
            if (audioMixer != null) audioMixer.SetFloat("MasterVolume", savedVolume);
        }

        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            float savedVolume = PlayerPrefs.GetFloat("MusicVolume");
            if (musicVolumeSlider != null) musicVolumeSlider.value = savedVolume;
            if (audioMixer != null) audioMixer.SetFloat("MusicVolume", savedVolume);
        }

        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            float savedVolume = PlayerPrefs.GetFloat("SFXVolume");
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = savedVolume;
            if (audioMixer != null) audioMixer.SetFloat("SFXVolume", savedVolume);
        }

        if (PlayerPrefs.HasKey("QualityLevel"))
        {
            int savedQuality = PlayerPrefs.GetInt("QualityLevel");
            if (qualityDropdown != null) qualityDropdown.value = savedQuality;
            QualitySettings.SetQualityLevel(savedQuality);
        }

        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
            if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;
            Screen.fullScreen = isFullscreen;
        }

        if (PlayerPrefs.HasKey("ResolutionIndex"))
        {
            int savedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex");
            if (resolutionDropdown != null) resolutionDropdown.value = savedResolutionIndex;
            Resolution resolution = _availableResolutions[savedResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }
}