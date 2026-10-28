using TMPro;
using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private GameObject newSlotPanel;
    [SerializeField] private GameObject loadSavePanel;
    [SerializeField] private TMP_InputField saveNameInput;
    [SerializeField] private TMP_Text saveNameText;
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private ConfirmDeleteUI deleteConfirmationUI;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (SaveGameManager.Instance == null) return;

        bool exists = SaveGameManager.Instance.SlotExists(slotIndex);

        if (newSlotPanel != null) newSlotPanel.SetActive(!exists);

        if (loadSavePanel != null) loadSavePanel.SetActive(exists);

        if (exists && saveNameText != null)
        {
            SaveGameManager.SaveData data = SaveGameManager.Instance.GetSlotData(slotIndex);
            saveNameText.text = data != null ? data.saveName : SaveGameManager.Instance.GetDefaultSlotName(slotIndex);
        }

        if (!exists && saveNameInput != null) saveNameInput.text = "";
    }

    public void CreateSave()
    {
        if (SaveGameManager.Instance == null || mainMenu == null) return;

        string saveName = string.IsNullOrWhiteSpace(saveNameInput.text) ? SaveGameManager.Instance.GetDefaultSlotName(slotIndex) : saveNameInput.text.Trim();

        if (!SaveGameManager.Instance.CreateSlot(slotIndex, saveName))
        {
            Refresh();
            return;
        }

        Refresh();
        mainMenu.StartSelectedSave();
    }

    public void PlaySave()
    {
        if (SaveGameManager.Instance == null || mainMenu == null) return;

        if (SaveGameManager.Instance.SelectSlot(slotIndex)) mainMenu.StartSelectedSave();
        else Refresh();
    }

    public void DeleteSave()
    {
        if (SaveGameManager.Instance == null) return;

        SaveGameManager.Instance.DeleteSlot(slotIndex);
        Refresh();
    }

    public void ShowDeleteConfirmation()
    {
        if (deleteConfirmationUI != null) deleteConfirmationUI.ShowConfirmation(slotIndex);
    }
}
