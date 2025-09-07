using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public class MenuController: MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Slider musicVolSlider = null;
    [SerializeField] private Slider sfxVolSlider = null;
    public AudioMixer audioMixer;
    [SerializeField] private float musicVolDefault = 0.5f;
    [SerializeField] private float sfxVolDefault = 0.1f;
    private float _musicVolume;
    private float _sfxVolume;

    /*
    [Header("Gameplay Setting")]
    [SerializeField] private Slider mouseSensSlider = null;
    [SerializeField] private float mouseSensDefault = 5f;
    [SerializeField] private MouseSensitivityHandler mouseHandler;
    public float mouseSens = 5f;
    */

    [Header("Graphics Settings")]
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private TMP_Dropdown qualityDropdown = null;
    [SerializeField] private TMP_Dropdown resolutionDropdown = null;
    [SerializeField] private Toggle fullscreenToggle = null;
    [SerializeField] private float brightnessDefault = 0.0f;
    [SerializeField] private int qualityDefault = 0;
    [SerializeField] private int resolutionDefault = 0;
    [SerializeField] private bool isFullscreenDefault = false;
    private float _brightnessLevel;
    private int _qualityLevel;
    private int _resolution;
    private bool _isFullscreen;
    [SerializeField] private Volume postProcessingVolume;
    private ColorAdjustments colorAdjustments;
    private Resolution[] resolutions;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationPrompt = null;


    private bool isStartup = true;


    private void Start()
    {
        // Setup the screen sizes dropdown options
        resolutions = Screen.resolutions
            .Where(r => r.refreshRateRatio.value.Equals(Screen.currentResolution.refreshRateRatio.value))
            .ToArray();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options.Distinct().ToList());

        if (PlayerPrefs.HasKey("resolution"))
        {
            resolutionDropdown.value = PlayerPrefs.GetInt("resolution");
        }
        else 
        {
            resolutionDropdown.value = currentResolutionIndex;
        }

        resolutionDropdown.RefreshShownValue();

        // Set up defaults
        // Instead of blindly resetting, check if player has played before
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            if (PlayerPrefs.HasKey("hasPlayedBefore"))
            {
                LoadPlayerSettings();
            }
            else
            {
                ResetButton();
                PlayerPrefs.SetInt("hasPlayedBefore", 1); // Mark that player has played
            }
        }

        // Meant to help mute initial button sound at start of game bootup
        isStartup = false;

        Time.timeScale = 1f;
        PauseMenu.GameIsPaused = false;
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LoadPlayerSettings()
    {
        _musicVolume = PlayerPrefs.GetFloat("musicVolume", musicVolDefault);
        SetMusicVolume(_musicVolume);
        musicVolSlider.value = _musicVolume;

        _sfxVolume = PlayerPrefs.GetFloat("sfxVolume", sfxVolDefault);
        SetSFXVolume(_sfxVolume);
        sfxVolSlider.value = _sfxVolume;

        _brightnessLevel = PlayerPrefs.GetFloat("brightness", brightnessDefault);
        SetBrightness(_brightnessLevel);
        brightnessSlider.value = _brightnessLevel;

        _qualityLevel = PlayerPrefs.GetInt("quality", qualityDefault);
        SetQuality(_qualityLevel);
        qualityDropdown.value = _qualityLevel;

        _resolution = PlayerPrefs.GetInt("resolution", resolutionDefault);
        SetResolution(_resolution);
        resolutionDropdown.value = _resolution;

        _isFullscreen = PlayerPrefs.GetInt("isFullscreen", (isFullscreenDefault ? 1 : 0)) == 1;
        SetFullscreen(_isFullscreen);
        fullscreenToggle.isOn = _isFullscreen;

        GraphicsApply();
    }

    public void PlayButtonSound()
    {
        AudioManager.audioManagerInstance.PlaySFX(AudioManager.audioManagerInstance.button);
    }

    public void PlayButton()
    {
        PlayButtonSound();

        SceneManager.LoadScene(1);

        AudioManager.audioManagerInstance.StopMusic();
        AudioManager.audioManagerInstance.PlayMusic(AudioManager.audioManagerInstance.menuBackground);
    }

    public void QuitButton()
    {
        PlayButtonSound();

        Application.Quit();
    }

    public float SliderToDb(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        audioMixer.SetFloat("music_volume", SliderToDb(volume));
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = volume;
        audioMixer.SetFloat("sfx_volume", SliderToDb(volume));
    }

    public void MusicVolumeApply()
    {
        PlayerPrefs.SetFloat("musicVolume", _musicVolume);
    }

    public void SFXVolumeApply()
    {
        PlayerPrefs.SetFloat("sfxVolume", _sfxVolume);
    }

    /*
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSens = sensitivity;
        if (mouseHandler != null)
        {
            mouseHandler.SetSensitivity(sensitivity);
            mouseHandler.AdjustSpeed(sensitivity);
        }
    }

    public void MouseSensitivityApply()
    {
        PlayerPrefs.SetFloat("sensitivity", mouseSens);
    }
    */

    public void SetBrightness(float brightness)
    {
        _brightnessLevel = brightness;
    }

    public void SetFullscreen(bool isFullScreen)
    {
        if (!isStartup) PlayButtonSound();
        _isFullscreen = isFullScreen;
    }

    public void SetQuality(int qualityIndex)
    {
        if (!isStartup) PlayButtonSound();
        _qualityLevel = qualityIndex;
    }

    public void SetResolution(int resolutionIndex)
    {
        if (!isStartup) PlayButtonSound();
        _resolution = resolutionIndex;
    }

    public void GraphicsApply()
    {
        PlayerPrefs.SetFloat("brightness", _brightnessLevel);
        if (postProcessingVolume.profile.TryGet(out colorAdjustments))
        {
            if (colorAdjustments != null)
                colorAdjustments.postExposure.value = _brightnessLevel;
        }
        /*
        else
        {
            Debug.LogError("Color Adjustments not found in volume.");
        }
        */

        PlayerPrefs.SetInt("quality", _qualityLevel);
        QualitySettings.SetQualityLevel(_qualityLevel);

        PlayerPrefs.SetInt("isFullscreen", (_isFullscreen ? 1 : 0));
        Screen.fullScreenMode = _isFullscreen
        ? FullScreenMode.ExclusiveFullScreen
        : FullScreenMode.Windowed;

        PlayerPrefs.SetInt("resolution", _resolution);
        Resolution resolution = resolutions[_resolution];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }

    public void PlayConfirmationBox()
    {
        StartCoroutine(ConfirmationBox());
    }

    public IEnumerator ConfirmationBox()
    {
        confirmationPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(2);
        confirmationPrompt.SetActive(false);
    }

    public void ApplyButton()
    {
        MusicVolumeApply();
        SFXVolumeApply();

        // MouseSensitivityApply();

        GraphicsApply();

        PlayConfirmationBox();

        /*
        Debug.Log(_musicVolume);
        Debug.Log(_sfxVolume);
        Debug.Log(_brightnessLevel);
        Debug.Log(mouseSens);
        Debug.Log(_qualityLevel);
        Debug.Log(_resolution);
        Debug.Log(_isFullscreen);
        */

       // PrintURPSettings();
    }

    public void ResetButton()
    {
        /*
        SetMouseSensitivity(mouseSensDefault);
        mouseSensSlider.value = mouseSensDefault;
        MouseSensitivityApply();
        */

        // Debug.Log(brightnessDefault);
        SetBrightness(brightnessDefault);
        brightnessSlider.value = brightnessDefault;

        SetQuality(qualityDefault);
        qualityDropdown.value = qualityDefault;

        SetResolution(resolutionDefault);
        resolutionDropdown.value = resolutionDefault;

        SetFullscreen(isFullscreenDefault);
        fullscreenToggle.isOn = isFullscreenDefault;

        GraphicsApply();

        SetMusicVolume(musicVolDefault);
        musicVolSlider.value = musicVolDefault;
        MusicVolumeApply();

        SetSFXVolume(sfxVolDefault);
        sfxVolSlider.value = sfxVolDefault;
        SFXVolumeApply();
    }

    /*
    void PrintURPSettings()
    {
        var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset == null)
        {
            Debug.LogWarning("Current quality level does not use a URP Asset.");
            return;
        }

        Debug.Log("Render Scale: " + urpAsset.renderScale);
        Debug.Log("MSAA Level: " + urpAsset.msaaSampleCount);
        Debug.Log("Shadow Distance: " + urpAsset.shadowDistance);
    }
    */
}
