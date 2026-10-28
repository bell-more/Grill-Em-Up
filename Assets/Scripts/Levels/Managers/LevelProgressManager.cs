using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelProgressManager
{
    private static string trackedSceneName = "";
    private static int successfulOrders;
    private static int missedOrders;
    private static bool levelFinished;

    static LevelProgressManager()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static int GetLevelStars(int levelNumber)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && SharedLevelProgress.Instance != null)
        {
            switch (levelNumber)
            {
                case 1: return SharedLevelProgress.Instance.level1Stars.Value;
                case 2: return SharedLevelProgress.Instance.level2Stars.Value;
                case 3: return SharedLevelProgress.Instance.level3Stars.Value;
                default: return 0;
            }
        }

        SaveGameManager.SaveData save = GetCurrentSave();
        return save == null ? 0 : GetStarsFromSave(save, levelNumber);
    }

    public static int GetStars(int levelNumber)
    {
        return GetLevelStars(levelNumber);
    }

    public static int GetBestScore(int levelNumber)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && SharedLevelProgress.Instance != null)
        {
            switch (levelNumber)
            {
                case 1: return SharedLevelProgress.Instance.level1BestScore.Value;
                case 2: return SharedLevelProgress.Instance.level2BestScore.Value;
                case 3: return SharedLevelProgress.Instance.level3BestScore.Value;
                default: return 0;
            }
        }

        SaveGameManager.SaveData save = GetCurrentSave();
        if (save == null) return 0;

        switch (levelNumber)
        {
            case 1: return save.level1BestScore;
            case 2: return save.level2BestScore;
            case 3: return save.level3BestScore;
            default: return 0;
        }
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1) return true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && SharedLevelProgress.Instance != null)
        {
            switch (levelNumber)
            {
                case 2: return SharedLevelProgress.Instance.level2Unlocked.Value;
                case 3: return SharedLevelProgress.Instance.level3Unlocked.Value;
                default: return false;
            }
        }

        SaveGameManager.SaveData save = GetCurrentSave();
        if (save == null) return false;

        switch (levelNumber)
        {
            case 2: return save.level2Unlocked;
            case 3: return save.level3Unlocked;
            default: return false;
        }
    }

    private static SaveGameManager.SaveData GetCurrentSave()
    {
        if (SaveGameManager.Instance == null) return null;

        return SaveGameManager.Instance.CurrentSave;
    }

    private static int GetStarsFromSave(SaveGameManager.SaveData save, int levelNumber)
    {
        switch (levelNumber)
        {
            case 1: return save.level1Stars;
            case 2: return save.level2Stars;
            case 3: return save.level3Stars;
            default: return 0;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        trackedSceneName = scene.name;
        successfulOrders = 0;
        missedOrders = 0;
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

    public static void RegisterMissedOrder()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)return;

        EnsureSceneState();

        if (levelFinished) return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "3.Level1" || sceneName == "4.Level2") missedOrders++;
    }

    public static void FinishCurrentLevel()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        EnsureSceneState();

        if (levelFinished) return;

        string sceneName = SceneManager.GetActiveScene().name;
        int levelNumber;

        if (sceneName == "3.Level1") levelNumber = 1;
        else if (sceneName == "4.Level2") levelNumber = 2;
        else return;

        levelFinished = true;

        if (OrderManager.Instance != null) OrderManager.Instance.ClearOrders();

        if (LevelTimer.Instance != null) LevelTimer.Instance.FreezeGameClientRpc();

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;

        int stars = CalculateStars(coins);

        SaveGameManager.SaveData save = GetCurrentSave();

        if (save != null)
        {
            int previousStars = GetStarsFromSave(save, levelNumber);
            int previousScore = GetBestScore(levelNumber);

            SetStars(save, levelNumber, Mathf.Max(previousStars, stars));
            SetBestScore(save, levelNumber, Mathf.Max(previousScore, coins));

            save.level1Unlocked = true;
            save.level2Unlocked = save.level1Stars >= 1;
            save.level3Unlocked = save.level2Stars >= 1;

            SaveGameManager.Instance.SaveCurrentSlot();
            if (SharedLevelProgress.Instance != null) SharedLevelProgress.Instance.SyncFromHostSave();
        }

        if (LevelTimer.Instance != null && LevelTimer.Instance.IsServer) LevelTimer.Instance.isPlaying.Value = false;

        if (LevelTimer.Instance != null) LevelTimer.Instance.ShowResults(successfulOrders, missedOrders, coins, stars, levelNumber);
    }

    private static void SetStars(SaveGameManager.SaveData save, int levelNumber, int stars)
    {
        switch (levelNumber)
        {
            case 1: save.level1Stars = stars; break;
            case 2: save.level2Stars = stars; break;
            case 3: save.level3Stars = stars; break;
        }
    }

    private static void SetBestScore(SaveGameManager.SaveData save, int levelNumber, int score)
    {
        switch (levelNumber)
        {
            case 1: save.level1BestScore = score; break;
            case 2: save.level2BestScore = score; break;
            case 3: save.level3BestScore = score; break;
        }
    }

    private static int CalculateStars(int coins)
    {
        if (coins >= 150) return 3;
        if (coins >= 100) return 2;
        if (coins >= 50) return 1;
        return 0;
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
}
