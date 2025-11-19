/// <summary>
/// Represents the current phase of the game
/// </summary>
public enum GamePhase
{
    Waiting,        // Waiting for players to connect
    Setup,          // Initializing decks and hands
    TurnStart,      // Beginning of turn (draw card, gain energy)
    CardSelection,  // Players selecting cards to play
    CardReveal,     // Revealing selected cards
    Resolution,     // Resolving card powers and abilities
    TurnEnd,        // End of turn cleanup
    GameOver        // Game finished
}

/// <summary>
/// Helper methods for GamePhase
/// </summary>
public static class GamePhaseExtensions
{
    public static string GetPhaseName(this GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Waiting: return "Waiting for players...";
            case GamePhase.Setup: return "Setting up game...";
            case GamePhase.TurnStart: return "Turn Starting";
            case GamePhase.CardSelection: return "Select Your Cards";
            case GamePhase.CardReveal: return "Revealing Cards";
            case GamePhase.Resolution: return "Resolving Actions";
            case GamePhase.TurnEnd: return "Turn Ending";
            case GamePhase.GameOver: return "Game Over";
            default: return "Unknown";
        }
    }
}