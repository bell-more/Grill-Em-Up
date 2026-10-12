
using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private AudioMixerSnapshot audioMixerSnapshot;

    public void Play()
    {
        TransitionTo(audioMixerSnapshot);
        Invoke(nameof(DisableMainMenu), 1f);
        loadingGamePanel.SetActive(true);
        Invoke("LoadScene", 3f);
    }

    
    void TransitionTo(AudioMixerSnapshot snap, float fade = 1f)
    {
        snap.TransitionTo(fade);
    }


    public void Quit()
    {
        Application.Quit();
    }

    public void DisableMainMenu()
    {
        mainMenuPanel.SetActive(false);
    }
    public void LoadScene()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void Credits()
    {
        creditsPanel.SetActive(true);
    }

     public void Settings()
    {
        settingsPanel.SetActive(true);
    }


    public void Info()
    {
        infoPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        infoPanel.SetActive(false);
        creditsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }
}
