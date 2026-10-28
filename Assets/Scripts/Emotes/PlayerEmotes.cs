using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerEmotes : NetworkBehaviour
{
    //options
    [SerializeField] private GameObject emoteOptionsBubble;
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;

    //UI
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TMP_Text speechText;
    [SerializeField] private float displayDuration = 3f;

    public bool IsMenuOpen { get; private set; }

    private string[] emoteMessages = new string[]
    {
        "Cooking...",
        "Chopping...",
        "It's burnt!",
        "Help!",
        "I need an ingredient"
    };

    private Coroutine hideCoroutine;
    private int selectedIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (speechBubble != null) speechBubble.SetActive(false);
        if (emoteOptionsBubble != null) emoteOptionsBubble.SetActive(false);
        IsMenuOpen = false;
    }

    private void Update()
    {
        if (!IsOwner || Keyboard.current == null) return;

        if (Keyboard.current.nKey.wasPressedThisFrame && !speechBubble.activeSelf)
        {
            IsMenuOpen = true; 
            selectedIndex = 0;
            UpdateVisualSelection();

            if (emoteOptionsBubble != null) emoteOptionsBubble.SetActive(true);
        }

        if (IsMenuOpen) 
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                selectedIndex--;
                if (selectedIndex < 0) selectedIndex = emoteMessages.Length - 1;
                UpdateVisualSelection();
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                selectedIndex++;
                if (selectedIndex >= emoteMessages.Length) selectedIndex = 0;
                UpdateVisualSelection();
            }

            if (Keyboard.current.nKey.wasReleasedThisFrame)
            {
                IsMenuOpen = false;
                if (emoteOptionsBubble != null) emoteOptionsBubble.SetActive(false);

                ShowEmoteServerRpc(selectedIndex);
            }
        }
    }

    private void UpdateVisualSelection()
    {
        if (optionTexts == null || optionTexts.Length != emoteMessages.Length)
        {
            return;
        }

        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].color = (i == selectedIndex) ? selectedColor : normalColor;
            optionTexts[i].text = (i == selectedIndex) ? $"> {emoteMessages[i]} <" : emoteMessages[i];
        }
    }

    [Rpc(SendTo.Server)]
    private void ShowEmoteServerRpc(int emoteIndex)
    {
        ShowEmoteClientRpc(emoteIndex);
    }

    [Rpc(SendTo.Everyone)]
    private void ShowEmoteClientRpc(int emoteIndex)
    {
        if (emoteIndex < 0 || emoteIndex >= emoteMessages.Length) return;

        speechText.text = emoteMessages[emoteIndex];
        speechBubble.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideBubbleRoutine());
    }

    private IEnumerator HideBubbleRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        if (speechBubble != null) speechBubble.SetActive(false);
    }
}