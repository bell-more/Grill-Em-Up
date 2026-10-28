using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class VolumeSettingManager : MonoBehaviour
{
    public static VolumeSettingManager instance;

    [SerializeField] private AudioMixer mainMixer;
    float currentMusicVolume, currentRainVolume, currentSFXVolume, currentUIVolume, currentVoiceVolume;

    //GETTERS
    public float GetCurrentMusicVolume() => currentMusicVolume;
    public float GetCurrentRainVolume() => currentRainVolume;
    public float GetCurrentSFXVolume() => currentSFXVolume;
    public float GetCurrentUIVolume() => currentUIVolume;
    public float GetCurrentVoiceVolume() => currentVoiceVolume;



    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadVolume();
        ApplyVolume();
    }

    void Start()
    {
        LoadVolume();
        ApplyVolume();
    }

    private void LoadVolume()
    {
        currentMusicVolume = PlayerPrefs.GetFloat("MusicVolume", -38f);
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", -10f);
        currentUIVolume = PlayerPrefs.GetFloat("UIVolume", -14f);
    }

    private void ApplyVolume()
    {
        mainMixer.SetFloat("MusicVolume", ConvertVolume(currentMusicVolume));
        mainMixer.SetFloat("SFXVolume", ConvertVolume(currentSFXVolume));
        mainMixer.SetFloat("UIVolume", ConvertVolume(currentUIVolume));
    }

    public void SetVolume(float volume, string name)
    {
        mainMixer.SetFloat(name, ConvertVolume(volume));
        PlayerPrefs.SetFloat(name, volume);
        PlayerPrefs.Save();
    }

    private float ConvertVolume(float volume)
    {
        float res = volume <= -40f ? -80f : volume;
        return res;
    }

}
