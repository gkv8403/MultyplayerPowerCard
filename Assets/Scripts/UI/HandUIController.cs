using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the visual representation of the local player's hand.
/// Creates and destroys CardUI objects based on the local PlayerState.
/// </summary>
public class HandUIController : MonoBehaviour
{
    [Header("References")]
    public CardUI cardUIPrefab; // ASSIGN THIS: The CardUI prefab from the last step
    public RectTransform handContainer; // ASSIGN THIS: The parent RectTransform for the cards (e.g., a Horizontal Layout Group)

    private PlayerState localPlayer;

    // Dictionary to link a C# Card instance to its physical CardUI GameObject
    private Dictionary<Card, CardUI> cardToUI = new Dictionary<Card, CardUI>();

    private void Start()
    {
        // 1. Must wait until GameManager is initialized and localPlayer is set
        GameEvents.OnGameInitialized += InitializeHand;

        // 2. This event is triggered by PlayerState when a card is drawn or played
        GameEvents.OnHandSizeChanged += UpdateHandVisuals;
    }

    private void OnDestroy()
    {
        GameEvents.OnGameInitialized -= InitializeHand;
        GameEvents.OnHandSizeChanged -= UpdateHandVisuals;
    }

    /// <summary>
    /// Gets the local player reference and checks for initial cards.
    /// </summary>
    private void InitializeHand()
    {
        // Get the local player reference from the non-networked manager
        localPlayer = GameManager.Instance.GetLocalPlayer();
        if (localPlayer == null)
        {
            Debug.LogError("HandUIController: Could not find local PlayerState!");
            return;
        }

        // Initial update in case cards were drawn during the Setup phase
        UpdateHandVisuals(localPlayer.hand.Count, localPlayer.playerName);
    }

    /// <summary>
    /// Event handler for card draws/plays, synchronizing the visual hand with the C# model.
    /// </summary>
    private void UpdateHandVisuals(int newSize, string playerName)
    {
        // Only update the local player's hand
        if (localPlayer == null || localPlayer.playerName != playerName)
        {
            return;
        }

        List<Card> currentHand = localPlayer.GetHandCards();
        HashSet<Card> cardsToKeep = new HashSet<Card>(currentHand);
        List<Card> cardsToRemove = new List<Card>();

        // 1. Identify and remove cards that are no longer in hand
        foreach (var entry in cardToUI)
        {
            if (!cardsToKeep.Contains(entry.Key))
            {
                cardsToRemove.Add(entry.Key);
                // Clean up the GameObject
                Destroy(entry.Value.gameObject);
            }
        }

        foreach (Card card in cardsToRemove)
        {
            cardToUI.Remove(card);
        }

        // 2. Add new cards and update existing ones
        foreach (Card card in currentHand)
        {
            if (!cardToUI.ContainsKey(card))
            {
                // Instantiate the UI element and link it to the C# Card instance
                CardUI newCardUI = Instantiate(cardUIPrefab, handContainer);
                newCardUI.SetupCard(card, localPlayer);
                cardToUI.Add(card, newCardUI);
            }
            else
            {
                // Ensure existing cards reflect any runtime data changes (e.g., ability effects)
                cardToUI[card].UpdateRuntimeData();
                cardToUI[card].UpdateSelectionVisual(card.isSelected);
            }
        }
    }
}