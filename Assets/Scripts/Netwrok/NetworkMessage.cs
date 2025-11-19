using System;
using System.Collections.Generic;

/// <summary>
/// Network message types for game communication
/// All messages are JSON serializable
/// </summary>

// ==================== BASE MESSAGE ====================

[Serializable]
public class NetworkMessage
{
    public string messageType;
    public string senderId;
}

// ==================== GAME INITIALIZATION ====================

[Serializable]
public class StartGameMessage : NetworkMessage
{
    public string hostPlayerId;
    public string clientPlayerId;
    public string hostPlayerName;
    public string clientPlayerName;
    public int seed;
    public StartGameMessage()
    {
        messageType = "StartGame";
    }
}

// ==================== TURN MESSAGES ====================

[Serializable]
public class TurnStartMessage : NetworkMessage
{
    public int turnNumber;
    public int hostEnergy;
    public int clientEnergy;

    public TurnStartMessage()
    {
        messageType = "TurnStart";
    }
}

/// <summary>
/// NEW: Host broadcasts this after ability resolution to sync scores and played cards.
/// </summary>
[Serializable]
public class TurnResolvedMessage : NetworkMessage
{
    public int hostScore;
    public int clientScore;
    public List<int> hostPlayedCards;
    public List<int> clientPlayedCards;

    public TurnResolvedMessage()
    {
        messageType = "TurnResolved";
        hostPlayedCards = new List<int>();
        clientPlayedCards = new List<int>();
    }
}

// ==================== CARD PLAY MESSAGES ====================

[Serializable]
public class PlayCardsMessage : NetworkMessage
{
    public List<int> cardIds; // IDs of cards being played
    public string playerName;

    public PlayCardsMessage()
    {
        messageType = "PlayCards";
        cardIds = new List<int>();
    }
}

// ==================== GAME OVER MESSAGE ====================

/// <summary>
/// NEW: Host broadcasts this when the game ends.
/// </summary>
[Serializable]
public class GameEndMessage : NetworkMessage
{
    public string winner;
    public int hostScore;
    public int clientScore;

    public GameEndMessage()
    {
        messageType = "GameEnd";
    }
}

// ==================== SERIALIZER ====================

public static class NetworkMessageSerializer
{
    /// <summary>
    /// Serialize message to JSON string
    /// </summary>
    public static string Serialize<T>(T message) where T : NetworkMessage
    {
        return UnityEngine.JsonUtility.ToJson(message);
    }

    /// <summary>
    /// Deserialize JSON string to message
    /// </summary>
    public static T Deserialize<T>(string json) where T : NetworkMessage
    {
        return UnityEngine.JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// Get message type from JSON without full deserialization
    /// </summary>
    public static string GetMessageType(string json)
    {
        try
        {
            NetworkMessage baseMsg = UnityEngine.JsonUtility.FromJson<NetworkMessage>(json);
            return baseMsg?.messageType ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}