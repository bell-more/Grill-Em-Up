using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    private bool isPaused = false;

    public bool GetIsPaused() => isPaused;

    void Start()
    {
        Time.timeScale = 1;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                NoPause();
            }
            else
            {
                PauseMenu();
            }
        }
    }

    public GameObject GetPausePanel()
    {
        return pausePanel;
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void NoPause()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    public void PauseMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0;
        isPaused = true;
        pausePanel.SetActive(true);
    }
  
    public void SettingsMenu()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        settingsPanel.SetActive(false);
    }

    public void NextLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadSceneAsync((SceneManager.GetActiveScene().buildIndex)+1);
    }
    public void MainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadSceneAsync(0);
    }
}
