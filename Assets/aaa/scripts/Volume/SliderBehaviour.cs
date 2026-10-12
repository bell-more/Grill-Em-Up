
using UnityEngine;
using UnityEngine.UI;

public class SliderBehaviour : MonoBehaviour
{
    private Slider sliderComponent;
    [SerializeField] VolumeSliders.sliderType type;
    void Awake()
    {
        VolumeSliders settings = FindFirstObjectByType<VolumeSliders>();

        sliderComponent = GetComponent<Slider>();

        sliderComponent.onValueChanged.AddListener((volume) =>
        {
            switch (type)
            {
                case VolumeSliders.sliderType.Music: settings.ChangeVolume(volume, "MusicVolume");
                    break;
                case VolumeSliders.sliderType.Rain: settings.ChangeVolume(volume, "RainVolume");
                    break;
                case VolumeSliders.sliderType.SFX: settings.ChangeVolume(volume, "SFXVolume");
                 break;
                case VolumeSliders.sliderType.UI: settings.ChangeVolume(volume, "UIVolume");
                    break;
                case VolumeSliders.sliderType.Voice: settings.ChangeVolume(volume, "VoiceVolume");
                    break; 
            }
       
        });

        if(settings!=null) settings.SetSlider(sliderComponent, type);
    }
    
    private void Start()
    {
       VolumeSettingManager volumeSettingManager = VolumeSettingManager.instance;

        if (volumeSettingManager == null) return;

        switch (type)
            {
                case VolumeSliders.sliderType.Music: sliderComponent.value = volumeSettingManager.GetCurrentMusicVolume();
                    break;
                case VolumeSliders.sliderType.Rain: sliderComponent.value = volumeSettingManager.GetCurrentRainVolume();
                    break;
                case VolumeSliders.sliderType.SFX: sliderComponent.value = volumeSettingManager.GetCurrentSFXVolume();
                 break;
                case VolumeSliders.sliderType.UI: sliderComponent.value = volumeSettingManager.GetCurrentUIVolume();
                    break;
                case VolumeSliders.sliderType.Voice: sliderComponent.value = volumeSettingManager.GetCurrentVoiceVolume();
                    break; 
            }
    }
}
