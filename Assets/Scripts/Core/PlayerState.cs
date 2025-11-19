using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a player's state in the game
/// </summary>
[System.Serializable]
public class PlayerState
{
    [Header("Player Info")]
    public PlayerRef playerRef;
    public string playerName;
    public bool isLocalPlayer;

    [Header("Game Resources")]
    public int energy = 0;
    public int maxEnergy = 6;
    public int score = 0;

    [Header("Card Management")]
    public Deck deck;
    public Hand hand;
    public List<Card> playedCardsThisTurn = new List<Card>();

    [Header("Status Flags")]
    public bool hasSubmittedCards = false;
    public bool isBlockedThisTurn = false;

    public PlayerState(PlayerRef playerRef, string playerName, bool isLocalPlayer)
    {
        this.playerRef = playerRef;
        this.playerName = playerName;
        this.isLocalPlayer = isLocalPlayer;

        deck = new Deck(playerName);
        hand = new Hand(playerName);
    }

    public void InitializeDeck(int deckSize = 12)
    {
        deck.Initialize(deckSize);
        deck.Shuffle();
        Debug.Log($"{playerName} deck initialized with {deck.Count} cards");
    }

    public void DrawInitialHand(int count = 3)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard();
        }
        Debug.Log($"{playerName} drew initial hand of {hand.Count} cards");
    }

    public bool DrawCard()
    {
        if (deck.IsEmpty)
        {
            Debug.LogWarning($"{playerName} has no cards left in deck!");
            return false;
        }

        Card drawnCard = deck.DrawCard();
        if (drawnCard != null)
        {
            bool added = hand.AddCard(drawnCard);
            if (added)
            {
                GameEvents.TriggerCardDrawn(drawnCard.data, playerName);
                GameEvents.TriggerHandSizeChanged(hand.Count, playerName);
            }
            return added;
        }

        return false;
    }

    public void GainEnergy()
    {
        if (energy < maxEnergy)
        {
            energy++;
            Debug.Log($"{playerName} gained energy. Current: {energy}/{maxEnergy}");
            GameEvents.TriggerEnergyChanged(energy, maxEnergy, playerName);
        }
    }

    public void SpendEnergy(int amount)
    {
        energy -= amount;
        if (energy < 0) energy = 0;
        Debug.Log($"{playerName} spent {amount} energy. Remaining: {energy}");
        GameEvents.TriggerEnergyChanged(energy, maxEnergy, playerName);
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"{playerName} gained {points} points. Total score: {score}");
        GameEvents.TriggerScoreChanged(score, playerName);
    }

    public void RemoveScore(int points)
    {
        score -= points;
        if (score < 0) score = 0;
        Debug.Log($"{playerName} lost {points} points. Total score: {score}");
        GameEvents.TriggerScoreChanged(score, playerName);
    }

    public bool SelectCard(Card card)
    {
        if (hasSubmittedCards)
        {
            Debug.LogWarning($"{playerName} has already submitted cards!");
            return false;
        }

        if (!hand.Contains(card))
        {
            Debug.LogWarning($"Card {card.Name} not in {playerName}'s hand!");
            return false;
        }

        if (card.isSelected)
        {
            Debug.LogWarning($"Card {card.Name} already selected!");
            return false;
        }

        bool selected = hand.SelectCard(card);

        if (selected)
        {
            // Check if we can afford the total selection
            if (!hand.CanAffordSelected(energy))
            {
                hand.DeselectCard(card);
                Debug.LogWarning($"Cannot afford {card.Name}! Total cost too high.");
                return false;
            }

            Debug.Log($"{playerName} selected {card.Name}. Total cost: {hand.GetSelectedCost()}/{energy}");
        }

        return selected;
    }

    public void DeselectCard(Card card)
    {
        hand.DeselectCard(card);
        Debug.Log($"{playerName} deselected {card.Name}");
    }

    public bool ToggleCardSelection(Card card)
    {
        if (card.isSelected)
        {
            DeselectCard(card);
            return false;
        }
        else
        {
            return SelectCard(card);
        }
    }

    public void ClearSelection()
    {
        hand.ClearSelection();
        Debug.Log($"{playerName} cleared selection");
    }

    public bool SubmitCards()
    {
        if (hasSubmittedCards)
        {
            Debug.LogWarning($"{playerName} has already submitted!");
            return false;
        }

        List<Card> selectedCards = hand.GetSelectedCards();

        if (selectedCards.Count == 0)
        {
            Debug.LogWarning($"{playerName} has no cards selected!");
            // Allow empty submission (pass turn)
            hasSubmittedCards = true;
            GameEvents.TriggerPlayerSubmittedCards(playerName, 0);
            return true;
        }

        int totalCost = hand.GetSelectedCost();
        if (totalCost > energy)
        {
            Debug.LogError($"{playerName} cannot afford selected cards! Cost: {totalCost}, Energy: {energy}");
            return false;
        }

        // Move selected cards to played
        playedCardsThisTurn.Clear();
        foreach (Card card in selectedCards)
        {
            hand.RemoveCard(card);
            card.isPlayed = true;
            card.isSelected = false;
            playedCardsThisTurn.Add(card);
            GameEvents.TriggerCardPlayed(card.data, playerName);
        }

        // Spend energy
        SpendEnergy(totalCost);

        hasSubmittedCards = true;
        Debug.Log($"✅ {playerName} submitted {playedCardsThisTurn.Count} card(s) [Cost: {totalCost}]");
        GameEvents.TriggerPlayerSubmittedCards(playerName, playedCardsThisTurn.Count);

        return true;
    }

    public void ResetTurn()
    {
        hand.ClearSelection();
        playedCardsThisTurn.Clear();
        hasSubmittedCards = false;
        isBlockedThisTurn = false;

        Debug.Log($"{playerName} turn reset. Hand: {hand.Count}, Energy: {energy}");
    }

    public List<Card> GetSelectedCards()
    {
        return hand.GetSelectedCards();
    }

    public int GetSelectedCardsCost()
    {
        return hand.GetSelectedCost();
    }

    public List<Card> GetHandCards()
    {
        return hand.GetAllCards();
    }

    public string GetInfoString()
    {
        return $"{playerName} | Energy: {energy}/{maxEnergy} | Score: {score} | Hand: {hand.Count} | Deck: {deck.Count}";
    }
}