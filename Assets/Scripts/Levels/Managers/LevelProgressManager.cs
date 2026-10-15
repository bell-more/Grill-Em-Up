
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelProgressManager
{
    private static string trackedSceneName = "";
    private static int successfulOrders;
    private static bool levelFinished;

    private static int missedOrders;
    static LevelProgressManager()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        trackedSceneName = scene.name;
        successfulOrders = 0;
        levelFinished = false;
    }

    public static void RegisterSuccessfulOrder()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        EnsureSceneState();

        if (levelFinished) return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != "3.Level1" && sceneName != "4.Level2") return;

        successfulOrders++;

        if (sceneName == "3.Level1" && successfulOrders >= 3) FinishCurrentLevel();
    }

    public static void FinishCurrentLevel()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        EnsureSceneState();

        if (levelFinished)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        int levelNumber;

        if (sceneName == "3.Level1")
        {
            levelNumber = 1;
        }
        else if (sceneName == "4.Level2")
        {
            levelNumber = 2;
        }
        else return;

        levelFinished = true;

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;

        int stars = CalculateStars(coins);

        string starsKey = $"Level{levelNumber}Stars";
        string scoreKey = $"Level{levelNumber}BestScore";

        int previousStars = PlayerPrefs.GetInt(starsKey, 0);
        int previousScore = PlayerPrefs.GetInt(scoreKey, 0);

        PlayerPrefs.SetInt(starsKey, Mathf.Max(previousStars, stars));
        PlayerPrefs.SetInt(scoreKey, Mathf.Max(previousScore, coins));

        SaveUnlockState();
        PlayerPrefs.Save();

        if (LevelTimer.Instance != null && LevelTimer.Instance.IsServer) LevelTimer.Instance.isPlaying.Value = false;

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.ShowResults(successfulOrders, missedOrders, coins, stars, levelNumber);
        }
            
    }

    private static int CalculateStars(int coins)
    {
        if (coins >= 150) return 3;
        if (coins >= 100) return 2;
        if (coins >= 50) return 1;
        return 0;
    }

    private static void SaveUnlockState()
    {
        int level1Stars = PlayerPrefs.GetInt("Level1Stars", 0);
        int level2Stars = PlayerPrefs.GetInt("Level2Stars", 0);

        PlayerPrefs.SetInt("Level2Unlocked", level1Stars >= 1 ? 1 : 0);
        PlayerPrefs.SetInt("Level3Unlocked", level2Stars >= 1 ? 1 : 0);
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1) return true;

        if (levelNumber == 2) return PlayerPrefs.GetInt("Level2Unlocked", 0) == 1;

        if (levelNumber == 3) return PlayerPrefs.GetInt("Level3Unlocked", 0) == 1;

        return false;
    }

    public static int GetStars(int levelNumber)
    {
        return PlayerPrefs.GetInt($"Level{levelNumber}Stars", 0);
    }

    public static int GetBestScore(int levelNumber)
    {
        return PlayerPrefs.GetInt($"Level{levelNumber}BestScore", 0);
    }

    private static void EnsureSceneState()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (trackedSceneName != currentScene)
        {
            trackedSceneName = currentScene;
            successfulOrders = 0;
            missedOrders = 0;
            levelFinished = false;
        }
    }
    public static void RegisterMissedOrder()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        EnsureSceneState();

        if (levelFinished) return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "3.Level1" || sceneName == "4.Level2") missedOrders++;
    }
}
