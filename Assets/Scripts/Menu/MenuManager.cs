using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

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

    //obtener panel de pausa
    public GameObject GetPausePanel()
    {
        return pausePanel;
    }

    //EMPEZAR JUEGO
    public void Play()
    {
        Invoke("LoadScene", 1);
    }

    //CARGAR ESCENA
    public void LoadScene()
    {
        SceneManager.LoadSceneAsync(1);

    }

    //SALIR DEL JUEGO
    public void Quit()
    {
        Application.Quit();
    }

    //SALIR DE PAUSA
    public void NoPause()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    //PAUSA
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


    //NEXT LEVEL
    public void NextLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadSceneAsync((SceneManager.GetActiveScene().buildIndex)+1);
    }

    //MAIN
    public void MainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadSceneAsync(0);
    }
}
