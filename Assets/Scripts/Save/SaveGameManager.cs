using System;
using System.IO;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; }

    public const int TotalSlots = 3;

    public int SelectedSlot { get; private set; } = -1;
    public SaveData CurrentSave { get; private set; }

    [Serializable]
    public class SaveData
    {
        public string saveName;
        public int level1Stars;
        public int level2Stars;
        public int level3Stars;
        public int level1BestScore;
        public int level2BestScore;
        public int level3BestScore;
        public bool level1Unlocked = true;
        public bool level2Unlocked;
        public bool level3Unlocked;
    }

    private string GetSlotPath(int slot)
    {
        string playerName = GetPlayerInstanceName();
        string suffix = playerName == "Player1" ? "" : $"_{playerName}";
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot + 1}{suffix}.json");
    }

    public string GetDefaultSlotName(int slot)
    {
        return "New slot "+ slot + 1;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool SlotExists(int slot)
    {
        return slot >= 0 && slot < TotalSlots && File.Exists(GetSlotPath(slot));
    }

    public SaveData GetSlotData(int slot)
    {
        if (!SlotExists(slot)) return null;

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(GetSlotPath(slot)));

            if (data == null) return null;

            NormalizeData(data);

            if (string.IsNullOrWhiteSpace(data.saveName)) data.saveName = GetDefaultSlotName(slot);

            return data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public bool CreateSlot(int slot, string saveName)
    {
        if (slot < 0 || slot >= TotalSlots) return false;

        if (SlotExists(slot)) return false;

        SelectedSlot = slot;
        CurrentSave = new SaveData();
        CurrentSave.saveName = string.IsNullOrWhiteSpace(saveName) ? GetDefaultSlotName(slot) : saveName.Trim();

        SaveCurrentSlot();
        return true;
    }

    public bool SelectSlot(int slot)
    {
        if (slot < 0 || slot >= TotalSlots) return false;

        SaveData data = GetSlotData(slot);

        if (data == null) return false;

        SelectedSlot = slot;
        CurrentSave = data;
        SaveCurrentSlot();
        return true;
    }

    public void SaveCurrentSlot()
    {
        if (SelectedSlot < 0 || CurrentSave == null) return;

        try
        {
            NormalizeData(CurrentSave);
            File.WriteAllText(GetSlotPath(SelectedSlot), JsonUtility.ToJson(CurrentSave, true));
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving slot " + e);
        }
    }

    public void DeleteSlot(int slot)
    {
        if (slot < 0 || slot >= TotalSlots) return;
        string path = GetSlotPath(slot);
        try
        {
            if (File.Exists(path)) File.Delete(path);
            if (SelectedSlot == slot)
            {
                SelectedSlot = -1;
                CurrentSave = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error deleting slot "+e);
        }
    }

    private string GetPlayerInstanceName()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-name") return args[i + 1];
        }
        return "Player1";
    }

    private void NormalizeData(SaveData data)
    {
        data.level1Unlocked = true;
        data.level2Unlocked = data.level1Stars >= 1;
        data.level3Unlocked = data.level2Stars >= 1;
    }

}
