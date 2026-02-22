using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

/// <summary>
/// Main game manager - handles game flow and state
/// FIXED: Game continues until disconnect, not turn limit
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int maxTurns = 999; // CHANGED: Set to 999 to effectively remove limit
    public float turnTimeLimit = 30f;
    public float revealDuration = 3f; // Time to show revealed cards

    [Header("Managers")]
    public TurnManager turnManager;
    public AbilityResolver abilityResolver;

    [Header("Current Game State")]
    public GamePhase currentPhase = GamePhase.Waiting;
    public int currentTurn = 0;
    public float turnTimeRemaining = 0f;

    [Header("Players")]
    public PlayerState hostPlayer;
    public PlayerState clientPlayer;

    [Header("Game End Settings")]
    public bool endOnDeckEmpty = false; // Optional: end when both decks empty

    private bool isGameStarted = false;
    private bool isResolvingTurn = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (turnManager == null)
        {
            turnManager = gameObject.AddComponent<TurnManager>();
            turnManager.turnTimeLimit = turnTimeLimit;
            turnManager.maxTurns = maxTurns;
        }

        if (abilityResolver == null)
        {
            abilityResolver = gameObject.AddComponent<AbilityResolver>();
        }
    }

    private void Update()
    {
        if (turnManager != null && turnManager.isTimerActive)
        {
            turnTimeRemaining = turnManager.timeRemaining;
        }
    }

    public void StartMatch(PlayerState p1, PlayerState p2)
    {
        if (isGameStarted) return;
        isGameStarted = true;

        hostPlayer = p1;
        clientPlayer = p2;

        Debug.Log("=== STARTING MATCH ===");
        SetPhase(GamePhase.Setup);

        // Initialize decks with same seed for both players
        hostPlayer.InitializeDeck(12);
        clientPlayer.InitializeDeck(12);

        hostPlayer.DrawInitialHand(3);
        clientPlayer.DrawInitialHand(3);

        hostPlayer.GainEnergy();
        clientPlayer.GainEnergy();

        GameEvents.TriggerGameInitialized();

        // Start first turn after a short delay
        Invoke(nameof(StartNewTurn), 1f);
    }

    public void StartNewTurn()
    {
        if (isResolvingTurn)
        {
            Debug.LogWarning("Already resolving turn, ignoring StartNewTurn call");
            return;
        }

        // REMOVED: Turn limit check - game continues until disconnect
        // if (CheckForGameEnd()) { EndGame(); return; }

        turnManager.IncrementTurn();
        currentTurn = turnManager.currentTurn;

        Debug.Log($"=== STARTING TURN {currentTurn} ===");

        SetPhase(GamePhase.TurnStart);

        // Reset both players
        hostPlayer.ResetTurn();
        clientPlayer.ResetTurn();

        // Draw and gain energy (skip on turn 1 as initial hand/energy already set)
        if (currentTurn > 1)
        {
            hostPlayer.DrawCard();
            clientPlayer.DrawCard();

            hostPlayer.GainEnergy();
            clientPlayer.GainEnergy();
        }

        GameEvents.TriggerTurnStart(currentTurn);

        // Move to selection phase
        SetPhase(GamePhase.CardSelection);
        GameEvents.TriggerCardSelectionStart();

        // Start timer (HOST ONLY in networked game)
        bool isHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.isHost;
        if (isHost || NetworkGameManager.Instance == null)
        {
            turnManager.StartTimer();
        }

        Debug.Log($"Turn {currentTurn} started. Host Energy: {hostPlayer.energy}, Client Energy: {clientPlayer.energy}");
    }

    public void EndTurnWithResolution()
    {
        if (isResolvingTurn)
        {
            Debug.LogWarning("Already resolving, ignoring duplicate call");
            return;
        }

        if (currentPhase == GamePhase.Resolution || currentPhase == GamePhase.GameOver || currentPhase == GamePhase.CardReveal)
        {
            Debug.LogWarning($"Cannot resolve in phase: {currentPhase}");
            return;
        }

        isResolvingTurn = true;
        Debug.Log("=== ENDING TURN WITH RESOLUTION ===");

        turnManager.StopTimer();

        // Start coroutine for reveal -> resolution -> next turn
        StartCoroutine(ResolutionSequence());
    }

    private IEnumerator ResolutionSequence()
    {
        // Phase 1: Card Reveal
        SetPhase(GamePhase.CardReveal);

        List<CardData> hostPlayedData = hostPlayer.playedCardsThisTurn
            .Select(card => card.data)
            .ToList();

        List<CardData> clientPlayedData = clientPlayer.playedCardsThisTurn
            .Select(card => card.data)
            .ToList();

        Debug.Log($"Revealing cards - Host: {hostPlayedData.Count}, Client: {clientPlayedData.Count}");

        GameEvents.TriggerCardsRevealed(hostPlayedData, clientPlayedData);

        // Wait for reveal duration
        yield return new WaitForSeconds(revealDuration);

        // Phase 2: Resolution (HOST ONLY in networked game)
        bool isHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.isHost;
        if (isHost || NetworkGameManager.Instance == null)
        {
            SetPhase(GamePhase.Resolution);
            GameEvents.TriggerTurnResolved();

            Debug.Log($"Pre-Resolution Scores - Host: {hostPlayer.score}, Client: {clientPlayer.score}");
            Debug.Log($"Pre-Resolution Energy - Host: {hostPlayer.energy}, Client: {clientPlayer.energy}");

            // Run ability resolution
            abilityResolver.ResolveAllAbilities(hostPlayer, clientPlayer);

            Debug.Log($"Post-Resolution Scores - Host: {hostPlayer.score}, Client: {clientPlayer.score}");
            Debug.Log($"Post-Resolution Energy - Host: {hostPlayer.energy}, Client: {clientPlayer.energy}");
        }

        // Phase 3: Turn End
        SetPhase(GamePhase.TurnEnd);
        GameEvents.TriggerTurnEnd();

        yield return new WaitForSeconds(1f);

        // Reset flag
        isResolvingTurn = false;

        // NOTE: Game end check is now handled by NetworkGameManager
        // Game will only end on disconnect or deck exhaustion
    }

    public void OnTurnTimerExpired()
    {
        Debug.Log("⏱️ GameManager: Timer expired!");

        // Only HOST should process timer expiry in networked game
        bool isHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.isHost;
        if (!isHost && NetworkGameManager.Instance != null)
        {
            Debug.Log("CLIENT: Ignoring timer expiry, waiting for host");
            return;
        }

        // Force submit for players who haven't submitted
        if (!hostPlayer.hasSubmittedCards)
        {
            Debug.Log("Force submitting host cards");
            hostPlayer.SubmitCards();
        }
        if (!clientPlayer.hasSubmittedCards)
        {
            Debug.Log("Force submitting client cards");
            clientPlayer.SubmitCards();
        }

        EndTurnWithResolution();
    }

    public void SetPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        GameEvents.TriggerGamePhaseChanged(newPhase);
        Debug.Log($"--- Phase Changed: {newPhase.GetPhaseName()} ---");
    }

    /// <summary>
    /// CHANGED: Only check for deck empty if enabled, otherwise return false
    /// Game ends by NetworkGameManager checking for disconnects
    /// </summary>
    public bool CheckForGameEnd()
    {
        if (endOnDeckEmpty)
        {
            bool hostDeckEmpty = hostPlayer.deck.IsEmpty;
            bool clientDeckEmpty = clientPlayer.deck.IsEmpty;

            if (hostDeckEmpty && clientDeckEmpty)
            {
                Debug.Log("Both decks empty, game should end");
                return true;
            }
        }

        // Game continues indefinitely until disconnect
        return false;
    }

    public void EndGame()
    {
        Debug.Log("=== GAME ENDING ===");

        SetPhase(GamePhase.GameOver);
        turnManager.StopTimer();

        // Calculate winner based on current scores
        string winnerName = "Tie";
        if (hostPlayer.score > clientPlayer.score)
        {
            winnerName = hostPlayer.playerName;
        }
        else if (clientPlayer.score > hostPlayer.score)
        {
            winnerName = clientPlayer.playerName;
        }

        Debug.Log($"🏆 GAME OVER! Winner: {winnerName} | Host: {hostPlayer.score}, Client: {clientPlayer.score}");

        GameEvents.TriggerGameEnd(winnerName, hostPlayer.score, clientPlayer.score);

        // Send to network (HOST ONLY)
        bool isHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.isHost;
        if (isHost)
        {
            NetworkGameManager.Instance.SendGameEndMessage(winnerName, hostPlayer.score, clientPlayer.score);
        }
    }

    public PlayerState GetPlayerState(PlayerRef playerRef)
    {
        if (hostPlayer != null && hostPlayer.playerRef == playerRef)
            return hostPlayer;

        if (clientPlayer != null && clientPlayer.playerRef == playerRef)
            return clientPlayer;

        return null;
    }

    public PlayerState GetLocalPlayer()
    {
        if (hostPlayer != null && hostPlayer.isLocalPlayer)
            return hostPlayer;

        if (clientPlayer != null && clientPlayer.isLocalPlayer)
            return clientPlayer;

        return null;
    }

    public PlayerState GetOpponentPlayer()
    {
        if (hostPlayer != null && !hostPlayer.isLocalPlayer)
            return hostPlayer;

        if (clientPlayer != null && !clientPlayer.isLocalPlayer)
            return clientPlayer;

        return null;
    }

    public void SubmitPlayerCards(PlayerRef playerRef)
    {
        PlayerState player = GetPlayerState(playerRef);
        if (player != null && player.SubmitCards())
        {
            Debug.Log($"✅ {player.playerName} submitted cards successfully");
        }
    }

    /// <summary>
    /// NEW: Force end game (for manual forfeit or disconnect)
    /// </summary>
    public void ForceEndGame(string reason)
    {
        Debug.Log($"⚠️ Force ending game: {reason}");
        EndGame();
    }
}