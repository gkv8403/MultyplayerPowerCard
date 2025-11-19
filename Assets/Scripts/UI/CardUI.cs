using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the visual representation and interaction of a single card in the Hand UI.
/// </summary>
public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text powerText;
    public TMP_Text abilitiesText;
    public Button cardButton;

    [Header("Visuals")]
    public Image cardBorderImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color affordableColor = Color.green;
    public Color notAffordableColor = Color.red;

    private Card cardInstance;
    private PlayerState localPlayer;
    private bool isInitialized = false;

    private void Awake()
    {
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
        }
    }

    public void SetupCard(Card card, PlayerState localPlayerState)
    {
        cardInstance = card;
        localPlayer = localPlayerState;
        isInitialized = true;

        // Update UI
        nameText.text = card.Name;
        UpdateRuntimeData();

        string abilitiesList = "";
        if (card.Abilities != null && card.Abilities.Count > 0)
        {
            abilitiesList = string.Join("\n", card.Abilities);
        }
        else
        {
            abilitiesList = "None";
        }
        abilitiesText.text = abilitiesList;

        UpdateSelectionVisual(cardInstance.isSelected);

        Debug.Log($"CardUI Setup: {card.Name} (ID: {card.ID})");
    }

    // IPointerClickHandler implementation for more reliable click detection
    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked();
    }

    private void OnCardClicked()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("CardUI not initialized!");
            return;
        }

        if (localPlayer == null || cardInstance == null)
        {
            Debug.LogWarning("CardUI: Missing references!");
            return;
        }

        if (GameManager.Instance.currentPhase != GamePhase.CardSelection)
        {
            Debug.LogWarning($"Cannot select card during phase: {GameManager.Instance.currentPhase}");
            return;
        }

        if (localPlayer.hasSubmittedCards)
        {
            Debug.LogWarning("Already submitted cards this turn!");
            return;
        }

        Debug.Log($"Card clicked: {cardInstance.Name} (Selected: {cardInstance.isSelected})");

        // Toggle selection
        bool newState = localPlayer.ToggleCardSelection(cardInstance);

        // Update visual immediately
        UpdateSelectionVisual(cardInstance.isSelected);

        // Update UI
        GameEvents.TriggerUIUpdateRequested();

        Debug.Log($"Card {cardInstance.Name} is now {(cardInstance.isSelected ? "SELECTED" : "DESELECTED")}");
    }

    public void UpdateSelectionVisual(bool isSelected)
    {
        if (cardBorderImage != null)
        {
            Color targetColor = normalColor;

            if (isSelected)
            {
                targetColor = selectedColor;
            }
            else if (localPlayer != null)
            {
                // Show affordability hint
                bool canAfford = cardInstance.Cost <= localPlayer.energy;
                targetColor = canAfford ? affordableColor : notAffordableColor;
            }

            cardBorderImage.color = targetColor;
        }

        // Update button interactability
        if (cardButton != null)
        {
            cardButton.interactable = !localPlayer.hasSubmittedCards;
        }
    }

    public void UpdateRuntimeData()
    {
        if (cardInstance == null) return;

        costText.text = cardInstance.Cost.ToString();
        powerText.text = cardInstance.Power.ToString();
    }

    private void OnDestroy()
    {
        isInitialized = false;
    }
}