using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VolumeSliders : MonoBehaviour
{
     [SerializeField] private Slider musicSlider, rainSlider, sfxSlider, UISlider, voiceSlider;

    void Start()
    {
        SetSliders();
    }

    public enum sliderType { Music, Rain, SFX, UI, Voice };
    public void SetSlider(Slider s, sliderType type) 
    {
        switch (type)
        {
            case sliderType.Music: musicSlider = s;
                break;
            case sliderType.Rain: rainSlider = s;
                break;
            case sliderType.SFX: sfxSlider = s;
                break;
            case sliderType.UI: UISlider = s;
                break;
            case sliderType.Voice: voiceSlider = s;
                break;
        }
    }

    public void ChangeVolume(float volume, string name)
    {
        VolumeSettingManager.instance.SetVolume(volume, name);
    }

    private void SetSliders()
    {
        float musicVolume = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : 1f;
        float rainVolume = PlayerPrefs.HasKey("RainVolume") ? PlayerPrefs.GetFloat("RainVolume") : 1f;
        float SFXVolume = PlayerPrefs.HasKey("SFXVolume") ? PlayerPrefs.GetFloat("SFXVolume") : 1f;
        float UIVolume = PlayerPrefs.HasKey("UIVolume") ? PlayerPrefs.GetFloat("UIVolume") : 1f;
        float voiceVolume = PlayerPrefs.HasKey("VoiceVolume") ? PlayerPrefs.GetFloat("VoiceVolume") : 1f;

        if (musicSlider != null) musicSlider.value = musicVolume;
        if(rainSlider!=null) rainSlider.value = rainVolume;
        if (sfxSlider != null) sfxSlider.value = SFXVolume;
        if (UISlider != null) UISlider.value = UIVolume;
        if(voiceSlider!=null) voiceSlider.value = voiceVolume;
    }
}
