using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages loading and accessing card data from JSON
/// </summary>
public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance;

    [Header("Card Data")]
    public TextAsset cardDataJson; // Assign in Inspector

    private Dictionary<int, CardData> cardDictionary = new Dictionary<int, CardData>();
    private List<CardData> allCards = new List<CardData>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCards();
    }

    /// <summary>
    /// Load cards from JSON file
    /// </summary>
    public void LoadCards()
    {
        if (cardDataJson == null)
        {
            Debug.LogError("CardDatabase: No JSON file assigned!");
            CreateDefaultCards(); // Fallback
            return;
        }

        try
        {
            // Parse JSON
            CardDataList cardList = JsonUtility.FromJson<CardDataList>(cardDataJson.text);

            if (cardList == null || cardList.cards == null || cardList.cards.Count == 0)
            {
                Debug.LogError("CardDatabase: Failed to parse JSON or no cards found!");
                CreateDefaultCards();
                return;
            }

            // Store cards
            allCards = cardList.cards;
            cardDictionary.Clear();

            foreach (var card in allCards)
            {
                cardDictionary[card.id] = card;
            }

            Debug.Log($"CardDatabase: Loaded {allCards.Count} cards from JSON");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CardDatabase: Error loading JSON: {e.Message}");
            CreateDefaultCards();
        }
    }

    /// <summary>
    /// Get card by ID
    /// </summary>
    public CardData GetCard(int cardId)
    {
        if (cardDictionary.ContainsKey(cardId))
        {
            return cardDictionary[cardId];
        }

        Debug.LogWarning($"CardDatabase: Card with ID {cardId} not found!");
        return null;
    }

    /// <summary>
    /// Get all cards
    /// </summary>
    public List<CardData> GetAllCards()
    {
        return new List<CardData>(allCards);
    }

    /// <summary>
    /// Get a random card
    /// </summary>
    public CardData GetRandomCard()
    {
        if (allCards.Count == 0) return null;
        return allCards[Random.Range(0, allCards.Count)];
    }

    /// <summary>
    /// Create a deck of 12 random cards (can have duplicates)
    /// </summary>
    public List<CardData> CreateRandomDeck(int deckSize = 12)
    {
        List<CardData> deck = new List<CardData>();

        for (int i = 0; i < deckSize; i++)
        {
            deck.Add(GetRandomCard());
        }

        return deck;
    }

    /// <summary>
    /// Fallback: Create default cards if JSON fails
    /// </summary>
    private void CreateDefaultCards()
    {
        Debug.LogWarning("CardDatabase: Creating default cards as fallback");

        allCards = new List<CardData>
        {
            new CardData(1, "Shield Bearer", 2, 3, new List<string> { "BlockNextAttack" }),
            new CardData(2, "Swift Blade", 1, 2, new List<string> { "DrawExtraCard" }),
            new CardData(3, "Power Strike", 3, 5, new List<string> { "DoublePower" }),
            new CardData(4, "Thief", 2, 2, new List<string> { "StealPoints" }),
            new CardData(5, "Guardian", 1, 1, new List<string> { "GainPoints" }),
            new CardData(6, "Warrior", 2, 4, new List<string>()),
            new CardData(7, "Mage", 3, 3, new List<string> { "GainPoints" }),
            new CardData(8, "Assassin", 4, 6, new List<string> { "StealPoints" }),
        };

        cardDictionary.Clear();
        foreach (var card in allCards)
        {
            cardDictionary[card.id] = card;
        }
    }

    /// <summary>
    /// Debug: Print all cards
    /// </summary>
    [ContextMenu("Print All Cards")]
    public void PrintAllCards()
    {
        Debug.Log("=== All Cards ===");
        foreach (var card in allCards)
        {
            Debug.Log(card.ToString());
        }
    }
}