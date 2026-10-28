using Unity.Netcode;

public class SharedLevelProgress : NetworkBehaviour
{
    public static SharedLevelProgress Instance { get; private set; }

    public NetworkVariable<int> level1Stars = new NetworkVariable<int>(0);
    public NetworkVariable<int> level2Stars = new NetworkVariable<int>(0);
    public NetworkVariable<int> level3Stars = new NetworkVariable<int>(0);
    public NetworkVariable<int> level1BestScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> level2BestScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> level3BestScore = new NetworkVariable<int>(0);
    public NetworkVariable<bool> level2Unlocked = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> level3Unlocked = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer) SyncFromHostSave();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    public void SyncFromHostSave()
    {
        if (!IsServer || SaveGameManager.Instance == null || SaveGameManager.Instance.CurrentSave == null) return;

        SaveGameManager.SaveData save = SaveGameManager.Instance.CurrentSave;
        level1Stars.Value = save.level1Stars;
        level2Stars.Value = save.level2Stars;
        level3Stars.Value = save.level3Stars;
        level1BestScore.Value = save.level1BestScore;
        level2BestScore.Value = save.level2BestScore;
        level3BestScore.Value = save.level3BestScore;
        level2Unlocked.Value = save.level2Unlocked;
        level3Unlocked.Value = save.level3Unlocked;
    }
}