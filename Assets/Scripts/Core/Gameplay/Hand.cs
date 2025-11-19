using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a player's hand of cards
/// </summary>
public class Hand
{
    private List<Card> cards = new List<Card>();
    private List<Card> selectedCards = new List<Card>();
    public string ownerName;
    public int maxHandSize = 10; // Prevent hand overflow

    public Hand(string ownerName)
    {
        this.ownerName = ownerName;
    }

    /// <summary>
    /// Get current hand size
    /// </summary>
    public int Count => cards.Count;

    /// <summary>
    /// Get selected cards count
    /// </summary>
    public int SelectedCount => selectedCards.Count;

    /// <summary>
    /// Check if hand is empty
    /// </summary>
    public bool IsEmpty => cards.Count == 0;

    /// <summary>
    /// Check if hand is full
    /// </summary>
    public bool IsFull => cards.Count >= maxHandSize;

    /// <summary>
    /// Add card to hand
    /// </summary>
    public bool AddCard(Card card)
    {
        if (IsFull)
        {
            Debug.LogWarning($"{ownerName}'s hand is full! Cannot add {card.Name}");
            return false;
        }

        cards.Add(card);
        GameEvents.TriggerHandSizeChanged(cards.Count, ownerName);
        return true;
    }

    /// <summary>
    /// Remove card from hand
    /// </summary>
    public bool RemoveCard(Card card)
    {
        bool removed = cards.Remove(card);
        if (removed)
        {
            selectedCards.Remove(card); // Also remove from selection
            GameEvents.TriggerHandSizeChanged(cards.Count, ownerName);
        }
        return removed;
    }

    /// <summary>
    /// Select a card for playing
    /// </summary>
    public bool SelectCard(Card card)
    {
        if (!cards.Contains(card))
        {
            Debug.LogWarning($"Card {card.Name} not in {ownerName}'s hand!");
            return false;
        }

        if (selectedCards.Contains(card))
        {
            Debug.LogWarning($"Card {card.Name} already selected!");
            return false;
        }

        selectedCards.Add(card);
        card.isSelected = true;

        Debug.Log($"{ownerName} selected: {card.Name}");
        GameEvents.TriggerCardSelected(card.data, ownerName);

        return true;
    }

    /// <summary>
    /// Deselect a card
    /// </summary>
    public bool DeselectCard(Card card)
    {
        if (!selectedCards.Contains(card))
        {
            return false;
        }

        selectedCards.Remove(card);
        card.isSelected = false;

        Debug.Log($"{ownerName} deselected: {card.Name}");
        GameEvents.TriggerCardDeselected(card.data, ownerName);

        return true;
    }

    /// <summary>
    /// Toggle card selection
    /// </summary>
    public bool ToggleCard(Card card)
    {
        if (card.isSelected)
        {
            return DeselectCard(card);
        }
        else
        {
            return SelectCard(card);
        }
    }

    /// <summary>
    /// Clear all selections
    /// </summary>
    public void ClearSelection()
    {
        foreach (Card card in selectedCards)
        {
            card.isSelected = false;
        }
        selectedCards.Clear();
    }

    /// <summary>
    /// Get all cards in hand
    /// </summary>
    public List<Card> GetAllCards()
    {
        return new List<Card>(cards);
    }

    /// <summary>
    /// Get selected cards
    /// </summary>
    public List<Card> GetSelectedCards()
    {
        return new List<Card>(selectedCards);
    }

    /// <summary>
    /// Get total cost of selected cards
    /// </summary>
    public int GetSelectedCost()
    {
        int totalCost = 0;
        foreach (Card card in selectedCards)
        {
            totalCost += card.Cost;
        }
        return totalCost;
    }

    /// <summary>
    /// Check if can afford selected cards with given energy
    /// </summary>
    public bool CanAffordSelected(int availableEnergy)
    {
        return GetSelectedCost() <= availableEnergy;
    }

    /// <summary>
    /// Get card by index
    /// </summary>
    public Card GetCard(int index)
    {
        if (index < 0 || index >= cards.Count)
        {
            return null;
        }
        return cards[index];
    }

    /// <summary>
    /// Check if hand contains card
    /// </summary>
    public bool Contains(Card card)
    {
        return cards.Contains(card);
    }

    /// <summary>
    /// Clear hand
    /// </summary>
    public void Clear()
    {
        cards.Clear();
        selectedCards.Clear();
    }

    /// <summary>
    /// Get hand info string
    /// </summary>
    public string GetInfoString()
    {
        return $"{ownerName}'s Hand: {Count} cards ({SelectedCount} selected)";
    }
}