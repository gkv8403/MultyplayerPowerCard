using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a deck of cards
/// </summary>
public class Deck
{
    private List<Card> cards = new List<Card>();
    public string ownerName;

    public Deck(string ownerName)
    {
        this.ownerName = ownerName;
    }

    /// <summary>
    /// Get remaining card count
    /// </summary>
    public int Count => cards.Count;

    /// <summary>
    /// Check if deck is empty
    /// </summary>
    public bool IsEmpty => cards.Count == 0;

    /// <summary>
    /// Initialize deck with random cards
    /// </summary>
    public void Initialize(int deckSize = 12)
    {
        cards.Clear();

        List<CardData> cardDataList = CardDatabase.Instance.CreateRandomDeck(deckSize);

        foreach (CardData data in cardDataList)
        {
            cards.Add(new Card(data));
        }

        Debug.Log($"{ownerName}'s deck initialized with {cards.Count} cards");
    }

    /// <summary>
    /// Shuffle the deck
    /// </summary>
    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }

        Debug.Log($"{ownerName}'s deck shuffled");
    }

    /// <summary>
    /// Draw top card from deck
    /// </summary>
    public Card DrawCard()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"{ownerName}'s deck is empty!");
            return null;
        }

        Card drawnCard = cards[0];
        cards.RemoveAt(0);

        Debug.Log($"{ownerName} drew: {drawnCard.Name}");
        return drawnCard;
    }

    /// <summary>
    /// Draw multiple cards
    /// </summary>
    public List<Card> DrawCards(int count)
    {
        List<Card> drawnCards = new List<Card>();

        for (int i = 0; i < count; i++)
        {
            Card card = DrawCard();
            if (card != null)
            {
                drawnCards.Add(card);
            }
        }

        return drawnCards;
    }

    /// <summary>
    /// Peek at top card without removing it
    /// </summary>
    public Card PeekTop()
    {
        if (IsEmpty) return null;
        return cards[0];
    }

    /// <summary>
    /// Add card to bottom of deck
    /// </summary>
    public void AddCard(Card card)
    {
        cards.Add(card);
    }

    /// <summary>
    /// Get all cards (for debugging)
    /// </summary>
    public List<Card> GetAllCards()
    {
        return new List<Card>(cards);
    }

    /// <summary>
    /// Clear deck
    /// </summary>
    public void Clear()
    {
        cards.Clear();
    }
}