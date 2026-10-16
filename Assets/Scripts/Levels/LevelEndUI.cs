
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEndUI : MonoBehaviour
{
    public static LevelEndUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject endPanel;

    [Header("Stars")]
    public Image[] starImages;
    public Sprite activeStarSprite;
    public Sprite inactiveStarSprite;

    [Header("Results")]
    public TMP_Text correctOrdersText;
    public TMP_Text missedOrdersText;
    public TMP_Text totalCoinsText;

    [Header("Buttons")]
    public GameObject hostButtons;
    public GameObject nextLevelButton;
    public string levelSelectScene = "LevelSelector";

    private int currentLevel;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (endPanel != null)
            endPanel.SetActive(false);
    }

    public void ShowResults(int correctOrders, int missedOrders, int coins, int stars, int levelNumber)
    {
        currentLevel = levelNumber;

        if (endPanel != null)
            endPanel.SetActive(true);

        if (correctOrdersText != null)
            correctOrdersText.text = correctOrders.ToString();

        if (missedOrdersText != null)
            missedOrdersText.text = missedOrders.ToString();

        if (totalCoinsText != null)
            totalCoinsText.text = coins.ToString();

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
                continue;

            starImages[i].sprite = i < stars ? activeStarSprite : inactiveStarSprite;
        }

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        if (hostButtons != null)
            hostButtons.SetActive(isHost);

        if (nextLevelButton != null)
            nextLevelButton.SetActive(isHost && stars >= 1 && levelNumber < 3);
    }

    public void Back()
    {
        LoadNetworkScene(levelSelectScene);
    }

    public void TryAgain()
    {
        LoadNetworkScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        if (currentLevel >= 3)
            return;

        if (LevelProgressManager.GetStars(currentLevel) < 1)
            return;

        string nextScene = currentLevel == 1 ? "4.Level2" : "5.Level3";

        LoadNetworkScene(nextScene);
    }

    private void LoadNetworkScene(string sceneName)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("No se puede cargar la escena: " + sceneName);
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}