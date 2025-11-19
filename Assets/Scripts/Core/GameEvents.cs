using System;
using System.Collections.Generic;

/// <summary>
/// Central event system for game communication
/// Uses event-driven architecture for decoupled components
/// </summary>
public static class GameEvents
{
    // ==================== GAME FLOW EVENTS ====================

    /// <summary>
    /// Fired when game is initialized and ready to start
    /// </summary>
    public static event Action OnGameInitialized;

    /// <summary>
    /// Fired at the start of each turn
    /// Params: turn number
    /// </summary>
    public static event Action<int> OnTurnStart;

    /// <summary>
    /// Fired when card selection phase begins
    /// </summary>
    public static event Action OnCardSelectionStart;

    /// <summary>
    /// Fired when cards are revealed
    /// Params: host cards, client cards
    /// </summary>
    public static event Action<List<CardData>, List<CardData>> OnCardsRevealed;

    /// <summary>
    /// Fired when turn is being resolved
    /// </summary>
    public static event Action OnTurnResolved;

    /// <summary>
    /// Fired when turn ends
    /// </summary>
    public static event Action OnTurnEnd;

    /// <summary>
    /// Fired when game ends
    /// Params: winner name, host score, client score
    /// </summary>
    public static event Action<string, int, int> OnGameEnd;

    // ==================== CARD EVENTS ====================

    /// <summary>
    /// Fired when a card is selected
    /// Params: card data, player name
    /// </summary>
    public static event Action<CardData, string> OnCardSelected;

    /// <summary>
    /// Fired when a card is deselected
    /// Params: card data, player name
    /// </summary>
    public static event Action<CardData, string> OnCardDeselected;

    /// <summary>
    /// Fired when a card is played
    /// Params: card data, player name
    /// </summary>
    public static event Action<CardData, string> OnCardPlayed;

    /// <summary>
    /// Fired when a card is drawn
    /// Params: card data, player name
    /// </summary>
    public static event Action<CardData, string> OnCardDrawn;

    // ==================== ABILITY EVENTS ====================

    /// <summary>
    /// Fired when an ability is triggered
    /// Params: ability type, card data, player name
    /// </summary>
    public static event Action<AbilityType, CardData, string> OnAbilityTriggered;

    /// <summary>
    /// Fired when GainPoints ability is used
    /// Params: points gained, player name
    /// </summary>
    public static event Action<int, string> OnPointsGained;

    /// <summary>
    /// Fired when StealPoints ability is used
    /// Params: points stolen, from player, to player
    /// </summary>
    public static event Action<int, string, string> OnPointsStolen;

    /// <summary>
    /// Fired when BlockNextAttack is activated
    /// Params: player name who activated block
    /// </summary>
    public static event Action<string> OnBlockActivated;

    /// <summary>
    /// Fired when DoublePower is activated
    /// Params: original power, new power, player name
    /// </summary>
    public static event Action<int, int, string> OnPowerDoubled;

    /// <summary>
    /// Fired when DrawExtraCard is triggered
    /// Params: player name
    /// </summary>
    public static event Action<string> OnExtraCardDrawn;

    // ==================== PLAYER STATE EVENTS ====================

    /// <summary>
    /// Fired when energy changes
    /// Params: new energy, max energy, player name
    /// </summary>
    public static event Action<int, int, string> OnEnergyChanged;

    /// <summary>
    /// Fired when score changes
    /// Params: new score, player name
    /// </summary>
    public static event Action<int, string> OnScoreChanged;

    /// <summary>
    /// Fired when hand size changes
    /// Params: new hand size, player name
    /// </summary>
    public static event Action<int, string> OnHandSizeChanged;

    // ==================== UI EVENTS ====================

    /// <summary>
    /// Fired when UI needs to update
    /// </summary>
    public static event Action OnUIUpdateRequested;

    /// <summary>
    /// Fired when status message should be displayed
    /// Params: message
    /// </summary>
    public static event Action<string> OnStatusMessageUpdated;

    /// <summary>
    /// Fired when turn timer updates
    /// Params: time remaining
    /// </summary>
    public static event Action<float> OnTurnTimerUpdated;

    /// <summary>
    /// Fired when local player submits cards and is waiting for opponent.
    /// Params: bool isWaiting (true/false)
    /// </summary>
    public static event Action<bool> OnWaitingForOpponent;
    // ==================== NETWORK EVENTS ====================

    /// <summary>
    /// Fired when player submits cards
    /// Params: player name, card count
    /// </summary>
    public static event Action<string, int> OnPlayerSubmittedCards;

    /// <summary>
    /// Fired when waiting for opponent
    /// </summary>
    //public static event Action OnWaitingForOpponent;

    /// <summary>
    /// Fired whenever the game phase changes
    /// Params: new phase
    /// </summary>
    public static event Action<GamePhase> OnGamePhaseChanged;

    // ==================== INVOKE METHODS (Public Triggers) ====================

    public static void TriggerGameInitialized() => OnGameInitialized?.Invoke();
    public static void TriggerTurnStart(int turnNumber) => OnTurnStart?.Invoke(turnNumber);
    public static void TriggerCardSelectionStart() => OnCardSelectionStart?.Invoke();
    public static void TriggerCardsRevealed(List<CardData> hostCards, List<CardData> clientCards)
        => OnCardsRevealed?.Invoke(hostCards, clientCards);
    public static void TriggerTurnResolved() => OnTurnResolved?.Invoke();
    public static void TriggerTurnEnd() => OnTurnEnd?.Invoke();
    public static void TriggerGameEnd(string winner, int hostScore, int clientScore)
        => OnGameEnd?.Invoke(winner, hostScore, clientScore);

    public static void TriggerCardSelected(CardData card, string playerName)
        => OnCardSelected?.Invoke(card, playerName);
    public static void TriggerCardDeselected(CardData card, string playerName)
        => OnCardDeselected?.Invoke(card, playerName);
    public static void TriggerCardPlayed(CardData card, string playerName)
        => OnCardPlayed?.Invoke(card, playerName);
    public static void TriggerCardDrawn(CardData card, string playerName)
        => OnCardDrawn?.Invoke(card, playerName);

    public static void TriggerAbilityTriggered(AbilityType ability, CardData card, string playerName)
        => OnAbilityTriggered?.Invoke(ability, card, playerName);
    public static void TriggerPointsGained(int points, string playerName)
        => OnPointsGained?.Invoke(points, playerName);
    public static void TriggerPointsStolen(int points, string fromPlayer, string toPlayer)
        => OnPointsStolen?.Invoke(points, fromPlayer, toPlayer);
    public static void TriggerBlockActivated(string playerName)
        => OnBlockActivated?.Invoke(playerName);
    public static void TriggerPowerDoubled(int originalPower, int newPower, string playerName)
        => OnPowerDoubled?.Invoke(originalPower, newPower, playerName);
    public static void TriggerExtraCardDrawn(string playerName)
        => OnExtraCardDrawn?.Invoke(playerName);

    public static void TriggerEnergyChanged(int newEnergy, int maxEnergy, string playerName)
        => OnEnergyChanged?.Invoke(newEnergy, maxEnergy, playerName);
    public static void TriggerScoreChanged(int newScore, string playerName)
        => OnScoreChanged?.Invoke(newScore, playerName);
    public static void TriggerHandSizeChanged(int newHandSize, string playerName)
        => OnHandSizeChanged?.Invoke(newHandSize, playerName);

    public static void TriggerUIUpdateRequested() => OnUIUpdateRequested?.Invoke();
    public static void TriggerStatusMessageUpdated(string message)
        => OnStatusMessageUpdated?.Invoke(message);
    public static void TriggerTurnTimerUpdated(float timeRemaining)
        => OnTurnTimerUpdated?.Invoke(timeRemaining);

    public static void TriggerPlayerSubmittedCards(string playerName, int cardCount)
        => OnPlayerSubmittedCards?.Invoke(playerName, cardCount);
    //  public static void TriggerWaitingForOpponent() => OnWaitingForOpponent?.Invoke();

    public static void TriggerGamePhaseChanged(GamePhase newPhase)
    => OnGamePhaseChanged?.Invoke(newPhase);
    /// <summary>
    /// Fired when local player submits cards and is waiting for opponent.
    /// Params: bool isWaiting (true/false)
    /// </summary>
    public static void TriggerWaitingForOpponent(bool isWaiting)
        => OnWaitingForOpponent?.Invoke(isWaiting);

    // ==================== HELPER METHODS ====================

    /// <summary>
    /// Clear all event subscribers (useful for cleanup)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnGameInitialized = null;
        OnTurnStart = null;
        OnCardSelectionStart = null;
        OnCardsRevealed = null;
        OnTurnResolved = null;
        OnTurnEnd = null;
        OnGameEnd = null;

        OnCardSelected = null;
        OnCardDeselected = null;
        OnCardPlayed = null;
        OnCardDrawn = null;

        OnAbilityTriggered = null;
        OnPointsGained = null;
        OnPointsStolen = null;
        OnBlockActivated = null;
        OnPowerDoubled = null;
        OnExtraCardDrawn = null;

        OnEnergyChanged = null;
        OnScoreChanged = null;
        OnHandSizeChanged = null;

        OnUIUpdateRequested = null;
        OnStatusMessageUpdated = null;
        OnTurnTimerUpdated = null;

        OnPlayerSubmittedCards = null;
        OnWaitingForOpponent = null;
    }

    /// <summary>
    /// Log event trigger for debugging
    /// </summary>
    public static void LogEvent(string eventName, params object[] parameters)
    {
#if UNITY_EDITOR
        string paramStr = parameters.Length > 0 ? string.Join(", ", parameters) : "no params";
        UnityEngine.Debug.Log($"[EVENT] {eventName} ({paramStr})");
#endif
    }
}