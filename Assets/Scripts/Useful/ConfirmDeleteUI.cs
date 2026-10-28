using TMPro;
using UnityEngine;

public class ConfirmDeleteUI : MonoBehaviour
{
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text confirmationText;

    private int selectedSlotIndex = -1;

    public void ShowConfirmation(int slotIndex)
    {
        if (SaveGameManager.Instance == null) return;

        SaveGameManager.SaveData data = SaveGameManager.Instance.GetSlotData(slotIndex);
        if (data == null) return;

        selectedSlotIndex = slotIndex;
        confirmationText.text = $"Delete save \"{data.saveName}\"?";
        confirmationPanel.SetActive(true);
    }

    public void ConfirmDelete()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= SaveGameManager.TotalSlots || SaveGameManager.Instance == null) return;

        SaveGameManager.Instance.DeleteSlot(selectedSlotIndex);
        selectedSlotIndex = -1;
        confirmationPanel.SetActive(false);
        SaveSlotUI[] slots = FindObjectsByType<SaveSlotUI>(FindObjectsSortMode.None);
        foreach (SaveSlotUI slot in slots) slot.Refresh();
    }

    public void CancelDelete()
    {
        selectedSlotIndex = -1;
        confirmationPanel.SetActive(false);
    }
}
