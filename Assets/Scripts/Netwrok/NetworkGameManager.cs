using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// FIXED: Properly handles network synchronization with host authority
/// All game logic runs on host, clients display results
/// </summary>
public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance;

    [Header("References")]
    public GameManager gameManager;
    public NetworkManager networkManager;

    [Header("Network State")]
    public bool isGameStarted = false;
    public bool isHost = false;

    [Header("Sync Settings")]
    public float timerSyncInterval = 2f; // Send timer updates every 2 seconds

    private bool waitingForResolution = false;
    private float lastTimerSyncTime = 0f;
    private int submitSequenceNumber = 0; // Prevent double submits

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (networkManager == null)
            networkManager = NetworkManager.Instance;
    }

    private void Start()
    {
        if (networkManager != null && networkManager.runner != null)
        {
            isHost = networkManager.runner.IsServer;
        }
    }

    private void Update()
    {
        // HOST: Periodically sync timer to clients
        if (isHost && gameManager != null && gameManager.turnManager.isTimerActive)
        {
            if (Time.time - lastTimerSyncTime >= timerSyncInterval)
            {
                SendTimerSyncMessage();
                lastTimerSyncTime = Time.time;
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCardSelected += OnCardSelected;
        GameEvents.OnCardDeselected += OnCardDeselected;
        GameEvents.OnPlayerSubmittedCards += OnPlayerSubmittedCards;
    }

    private void OnDisable()
    {
        GameEvents.OnCardSelected -= OnCardSelected;
        GameEvents.OnCardDeselected -= OnCardDeselected;
        GameEvents.OnPlayerSubmittedCards -= OnPlayerSubmittedCards;
    }

    private void OnCardSelected(CardData cardData, string playerName) { }
    private void OnCardDeselected(CardData cardData, string playerName) { }

    private void OnPlayerSubmittedCards(string playerName, int cardCount)
    {
        Debug.Log($"📤 OnPlayerSubmittedCards: {playerName} submitted {cardCount} cards");

        PlayerState localPlayer = gameManager.GetLocalPlayer();
        if (localPlayer == null || localPlayer.playerName != playerName) return;

        // Trigger waiting state locally
        GameEvents.TriggerWaitingForOpponent(true);
        GameEvents.TriggerStatusMessageUpdated($"You submitted {cardCount} cards. Waiting for opponent...");

        // CLIENT: Send to host for validation
        if (!isHost)
        {
            PlayCardsMessage msg = new PlayCardsMessage
            {
                playerName = playerName,
                cardIds = localPlayer.playedCardsThisTurn.Select(c => c.ID).ToList(),
                submitSequence = ++submitSequenceNumber
            };
            SendLocalMessageToServer(NetworkMessageSerializer.Serialize(msg));
            Debug.Log($"CLIENT: Sent PlayCards to host (seq: {msg.submitSequence})");
        }

        // HOST: Check for resolution
        if (isHost)
        {
            CheckForTurnResolution();
        }
    }

    /// <summary>
    /// HOST ONLY: Check if both players submitted and start resolution
    /// </summary>
    private void CheckForTurnResolution()
    {
        if (!isHost) return;

        if (waitingForResolution)
        {
            Debug.LogWarning("Already waiting for resolution");
            return;
        }

        if (gameManager.currentPhase != GamePhase.CardSelection)
        {
            Debug.LogWarning($"Not in selection phase: {gameManager.currentPhase}");
            return;
        }

        bool hostSubmitted = gameManager.hostPlayer.hasSubmittedCards;
        bool clientSubmitted = gameManager.clientPlayer.hasSubmittedCards;

        Debug.Log($"🔍 CheckForTurnResolution: Host={hostSubmitted}, Client={clientSubmitted}");

        if (hostSubmitted && clientSubmitted)
        {
            waitingForResolution = true;
            Debug.Log("✅ HOST: Both players submitted! Starting resolution...");

            // Stop timer
            gameManager.turnManager.StopTimer();

            // Run resolution on HOST only
            gameManager.EndTurnWithResolution();

            // Send results to client after resolution
            StartCoroutine(SendResolutionAfterDelay());
        }
    }

    /// <summary>
    /// HOST ONLY: Send resolution results to client after calculations complete
    /// </summary>
    private IEnumerator SendResolutionAfterDelay()
    {
        // Wait for reveal + resolution to complete on host
        yield return new WaitForSeconds(gameManager.revealDuration + 1.5f);

        Debug.Log("📤 HOST: Sending TurnResolved message to client");
        SendTurnResolvedMessage();

        yield return new WaitForSeconds(1f);

        waitingForResolution = false;

        // Check for game end (only host decides)
        if (ShouldEndGame())
        {
            Debug.Log("HOST: Game ending...");
            gameManager.EndGame();
        }
        else
        {
            Debug.Log("HOST: Starting next turn...");
            gameManager.StartNewTurn();
            SendTurnStartMessage();
        }
    }

    /// <summary>
    /// FIXED: Game ends only on disconnect or manual forfeit, NOT turn count
    /// </summary>
    private bool ShouldEndGame()
    {
        // Check for disconnections
        if (networkManager == null || networkManager.runner == null)
            return true;

        if (!networkManager.runner.IsConnectedToServer)
            return true;

        // Check if both players still connected
        var players = FindObjectsOfType<NetworkPlayer>();
        if (players.Length < 2)
        {
            Debug.Log("Player disconnected, ending game");
            return true;
        }

        // Check if both decks are empty (optional end condition)
        bool hostDeckEmpty = gameManager.hostPlayer.deck.IsEmpty;
        bool clientDeckEmpty = gameManager.clientPlayer.deck.IsEmpty;

        if (hostDeckEmpty && clientDeckEmpty)
        {
            Debug.Log("Both decks empty, ending game");
            return true;
        }

        // Game continues indefinitely until disconnect
        return false;
    }

    /// <summary>
    /// Process incoming network messages
    /// </summary>
    public void ReceiveMessage(string json)
    {
        string messageType = NetworkMessageSerializer.GetMessageType(json);
        Debug.Log($"📥 ReceiveMessage: {messageType}");

        switch (messageType)
        {
            case "StartGame":
                HandleStartGame(json);
                break;
            case "PlayCards":
                HandlePlayCards(json);
                break;
            case "TurnStart":
                HandleTurnStart(json);
                break;
            case "TurnResolved":
                HandleTurnResolved(json);
                break;
            case "GameEnd":
                HandleGameEnd(json);
                break;
            case "TimerSync":
                HandleTimerSync(json);
                break;
            case "ValidationError":
                HandleValidationError(json);
                break;
            default:
                Debug.LogWarning($"Unknown message: {messageType}");
                break;
        }
    }

    /// <summary>
    /// HOST ONLY: Initialize game for both players
    /// </summary>
    public void BeginGameSetup()
    {
        if (!networkManager.runner.IsServer) return;

        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        NetworkPlayer hostPl = null;
        NetworkPlayer clientPl = null;

        foreach (var p in players)
        {
            if (p.IsHost) hostPl = p;
            else clientPl = p;
        }

        if (hostPl != null && clientPl != null)
        {
            // Generate deterministic seed for both players
            int gameSeed = UnityEngine.Random.Range(0, 100000);

            StartGameMessage msg = new StartGameMessage
            {
                hostPlayerId = hostPl.Object.InputAuthority.ToString(),
                clientPlayerId = clientPl.Object.InputAuthority.ToString(),
                hostPlayerName = hostPl.PlayerName.ToString(),
                clientPlayerName = clientPl.PlayerName.ToString(),
                seed = gameSeed
            };

            string json = NetworkMessageSerializer.Serialize(msg);
            hostPl.SendGameMessageAsHost(json);

            Debug.Log($"📤 HOST: Game setup sent (Seed: {gameSeed})");
        }
    }

    /// <summary>
    /// BOTH: Initialize game state with shared seed
    /// </summary>
    private void HandleStartGame(string json)
    {
        Debug.Log("📥 HandleStartGame");
        StartGameMessage msg = NetworkMessageSerializer.Deserialize<StartGameMessage>(json);

        // Use shared seed for deterministic RNG
        UnityEngine.Random.InitState(msg.seed);

        bool amIServer = networkManager.runner.IsServer;

        PlayerState p1 = new PlayerState(PlayerRef.None, msg.hostPlayerName, amIServer);
        PlayerState p2 = new PlayerState(PlayerRef.None, msg.clientPlayerName, !amIServer);

        gameManager.StartMatch(p1, p2);

        if (GameUIController.Instance != null)
        {
            PlayerState myPlayerState = amIServer ? p1 : p2;
            GameUIController.Instance.Initialize(myPlayerState);
        }

        isGameStarted = true;
    }

    /// <summary>
    /// HOST ONLY: Validate and process card play from client
    /// </summary>
    private void HandlePlayCards(string json)
    {
        if (!isHost) return; // Only host processes this

        PlayCardsMessage msg = NetworkMessageSerializer.Deserialize<PlayCardsMessage>(json);
        Debug.Log($"📥 HOST: HandlePlayCards from {msg.playerName} (seq: {msg.submitSequence})");

        PlayerState submittingPlayer = FindPlayerByName(msg.playerName);

        if (submittingPlayer == null)
        {
            Debug.LogError($"Unknown player: {msg.playerName}");
            return;
        }

        // VALIDATION: Prevent double submit
        if (submittingPlayer.hasSubmittedCards)
        {
            Debug.LogWarning($"Player {msg.playerName} already submitted! Rejecting duplicate.");
            SendValidationError(msg.playerName, "Already submitted cards this turn");
            return;
        }

        // VALIDATION: Check if cards are in hand
        List<Card> handCards = submittingPlayer.GetHandCards();
        List<Card> requestedCards = handCards
            .Where(c => msg.cardIds.Contains(c.ID))
            .ToList();

        if (requestedCards.Count != msg.cardIds.Count)
        {
            Debug.LogError($"Card ownership validation failed for {msg.playerName}");
            SendValidationError(msg.playerName, "You don't own all requested cards");
            return;
        }

        // VALIDATION: Check energy cost
        int totalCost = requestedCards.Sum(c => c.Cost);
        if (totalCost > submittingPlayer.energy)
        {
            Debug.LogError($"Energy validation failed: {totalCost} > {submittingPlayer.energy}");
            SendValidationError(msg.playerName, $"Not enough energy: need {totalCost}, have {submittingPlayer.energy}");
            return;
        }

        // VALID: Apply card play
        submittingPlayer.ClearSelection();
        foreach (Card card in requestedCards)
        {
            submittingPlayer.SelectCard(card);
        }

        bool success = submittingPlayer.SubmitCards();

        if (success)
        {
            Debug.Log($"✅ HOST: {msg.playerName} submitted successfully");
            GameEvents.TriggerStatusMessageUpdated($"{msg.playerName} submitted!");
            CheckForTurnResolution();
        }
        else
        {
            Debug.LogError($"Failed to submit cards for {msg.playerName}");
            SendValidationError(msg.playerName, "Failed to submit cards");
        }
    }

    /// <summary>
    /// CLIENT ONLY: Receive and apply turn start from host
    /// </summary>
    private void HandleTurnStart(string json)
    {
        TurnStartMessage msg = NetworkMessageSerializer.Deserialize<TurnStartMessage>(json);
        Debug.Log($"📥 CLIENT: TurnStart - Turn {msg.turnNumber}");

        if (!isHost)
        {
            gameManager.currentTurn = msg.turnNumber;

            // Sync energy from host (authoritative)
            gameManager.hostPlayer.energy = msg.hostEnergy;
            gameManager.clientPlayer.energy = msg.clientEnergy;

            // Reset turn state
            PlayerState localPlayer = gameManager.GetLocalPlayer();
            if (localPlayer != null)
            {
                localPlayer.ResetTurn();

                // Draw card (skip on turn 1)
                if (msg.turnNumber > 1)
                {
                    localPlayer.DrawCard();
                }

                // Update UI with synced energy
                GameEvents.TriggerEnergyChanged(localPlayer.energy, localPlayer.maxEnergy, localPlayer.playerName);
            }

            // Sync phase
            gameManager.SetPhase(GamePhase.CardSelection);
            GameEvents.TriggerTurnStart(msg.turnNumber);
            GameEvents.TriggerCardSelectionStart();
            GameEvents.TriggerWaitingForOpponent(false);

            // Start timer (will be synced from host)
            gameManager.turnManager.StartTimer();

            Debug.Log($"CLIENT: Turn {msg.turnNumber} ready. Energy - Host: {msg.hostEnergy}, Client: {msg.clientEnergy}");
        }
    }

    /// <summary>
    /// CLIENT ONLY: Receive and display resolution results from host
    /// </summary>
    private void HandleTurnResolved(string json)
    {
        TurnResolvedMessage msg = NetworkMessageSerializer.Deserialize<TurnResolvedMessage>(json);
        Debug.Log($"📥 CLIENT: TurnResolved - Host: {msg.hostScore}, Client: {msg.clientScore}");

        if (!isHost)
        {
            // Show card reveal
            List<CardData> hostCards = msg.hostPlayedCards
                .Select(id => CardDatabase.Instance.GetCard(id))
                .Where(c => c != null)
                .ToList();

            List<CardData> clientCards = msg.clientPlayedCards
                .Select(id => CardDatabase.Instance.GetCard(id))
                .Where(c => c != null)
                .ToList();

            gameManager.SetPhase(GamePhase.CardReveal);
            GameEvents.TriggerCardsRevealed(hostCards, clientCards);

            // Apply results from host
            StartCoroutine(ApplyResolutionResults(msg));
        }
    }

    /// <summary>
    /// CLIENT ONLY: Apply host's resolution results
    /// </summary>
    private IEnumerator ApplyResolutionResults(TurnResolvedMessage msg)
    {
        yield return new WaitForSeconds(gameManager.revealDuration);

        // Apply authoritative scores from host
        gameManager.hostPlayer.score = msg.hostScore;
        gameManager.clientPlayer.score = msg.clientScore;

        // Apply energy changes from host
        gameManager.hostPlayer.energy = msg.hostEnergy;
        gameManager.clientPlayer.energy = msg.clientEnergy;

        PlayerState local = gameManager.GetLocalPlayer();
        PlayerState opponent = gameManager.GetOpponentPlayer();

        Debug.Log($"CLIENT: Applied scores - Host: {msg.hostScore}, Client: {msg.clientScore}");
        Debug.Log($"CLIENT: Applied energy - Host: {msg.hostEnergy}, Client: {msg.clientEnergy}");

        // Update UI
        if (local != null)
        {
            GameEvents.TriggerScoreChanged(local.score, local.playerName);
            GameEvents.TriggerEnergyChanged(local.energy, local.maxEnergy, local.playerName);
        }
        if (opponent != null)
        {
            GameEvents.TriggerScoreChanged(opponent.score, opponent.playerName);
        }

        gameManager.SetPhase(GamePhase.TurnEnd);
        GameEvents.TriggerTurnEnd();
        GameEvents.TriggerWaitingForOpponent(false);
    }

    /// <summary>
    /// BOTH: Handle game end message
    /// </summary>
    private void HandleGameEnd(string json)
    {
        GameEndMessage msg = NetworkMessageSerializer.Deserialize<GameEndMessage>(json);
        Debug.Log($"📥 GameEnd: Winner={msg.winner}, Host={msg.hostScore}, Client={msg.clientScore}");

        gameManager.SetPhase(GamePhase.GameOver);
        gameManager.turnManager.StopTimer();

        // Apply final authoritative scores
        gameManager.hostPlayer.score = msg.hostScore;
        gameManager.clientPlayer.score = msg.clientScore;

        GameEvents.TriggerScoreChanged(gameManager.hostPlayer.score, gameManager.hostPlayer.playerName);
        GameEvents.TriggerScoreChanged(gameManager.clientPlayer.score, gameManager.clientPlayer.playerName);
        GameEvents.TriggerGameEnd(msg.winner, msg.hostScore, msg.clientScore);
    }

    /// <summary>
    /// CLIENT ONLY: Sync timer from host
    /// </summary>
    private void HandleTimerSync(string json)
    {
        if (isHost) return; // Host doesn't need to sync from itself

        TimerSyncMessage msg = NetworkMessageSerializer.Deserialize<TimerSyncMessage>(json);

        // Apply host's authoritative timer value
        if (gameManager.turnManager.isTimerActive)
        {
            gameManager.turnManager.timeRemaining = msg.timeRemaining;
            GameEvents.TriggerTurnTimerUpdated(msg.timeRemaining);
        }
    }

    /// <summary>
    /// CLIENT ONLY: Handle validation error from host
    /// </summary>
    private void HandleValidationError(string json)
    {
        ValidationErrorMessage msg = NetworkMessageSerializer.Deserialize<ValidationErrorMessage>(json);
        Debug.LogError($"❌ Validation Error: {msg.errorMessage}");

        // Show error to player
        GameEvents.TriggerStatusMessageUpdated($"❌ Error: {msg.errorMessage}");

        // Re-enable submit button
        PlayerState localPlayer = gameManager.GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.hasSubmittedCards = false;
            GameEvents.TriggerWaitingForOpponent(false);
        }
    }

    // ==================== SEND METHODS ====================

    public void SendLocalMessageToServer(string json)
    {
        if (isHost) return;

        NetworkPlayer localNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (localNetPlayer != null)
        {
            localNetPlayer.RPC_SendToServer(json);
        }
    }

    /// <summary>
    /// HOST ONLY: Broadcast turn start to all clients
    /// </summary>
    public void SendTurnStartMessage()
    {
        if (!isHost) return;

        TurnStartMessage msg = new TurnStartMessage
        {
            turnNumber = gameManager.currentTurn,
            hostEnergy = gameManager.hostPlayer.energy,
            clientEnergy = gameManager.clientPlayer.energy,
            timeRemaining = gameManager.turnTimeLimit
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
            Debug.Log($"📤 HOST: Sent TurnStart - Turn {msg.turnNumber}, Energy: {msg.hostEnergy}/{msg.clientEnergy}");
        }
    }

    /// <summary>
    /// HOST ONLY: Send resolution results to clients
    /// </summary>
    public void SendTurnResolvedMessage()
    {
        if (!isHost) return;

        List<int> hostCards = gameManager.hostPlayer.playedCardsThisTurn.Select(c => c.ID).ToList();
        List<int> clientCards = gameManager.clientPlayer.playedCardsThisTurn.Select(c => c.ID).ToList();

        TurnResolvedMessage msg = new TurnResolvedMessage
        {
            hostScore = gameManager.hostPlayer.score,
            clientScore = gameManager.clientPlayer.score,
            hostEnergy = gameManager.hostPlayer.energy,
            clientEnergy = gameManager.clientPlayer.energy,
            hostPlayedCards = hostCards,
            clientPlayedCards = clientCards
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
            Debug.Log($"📤 HOST: Sent TurnResolved - Scores: {msg.hostScore}/{msg.clientScore}, Energy: {msg.hostEnergy}/{msg.clientEnergy}");
        }
    }

    /// <summary>
    /// HOST ONLY: Send game end message
    /// </summary>
    public void SendGameEndMessage(string winner, int hostScore, int clientScore)
    {
        if (!isHost) return;

        GameEndMessage msg = new GameEndMessage
        {
            winner = winner,
            hostScore = hostScore,
            clientScore = clientScore
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
            Debug.Log($"📤 HOST: Sent GameEnd - Winner: {winner}");
        }
    }

    /// <summary>
    /// HOST ONLY: Send timer sync to clients
    /// </summary>
    private void SendTimerSyncMessage()
    {
        if (!isHost) return;

        TimerSyncMessage msg = new TimerSyncMessage
        {
            timeRemaining = gameManager.turnManager.timeRemaining
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
        }
    }

    /// <summary>
    /// HOST ONLY: Send validation error to specific client
    /// </summary>
    private void SendValidationError(string playerName, string errorMessage)
    {
        if (!isHost) return;

        ValidationErrorMessage msg = new ValidationErrorMessage
        {
            playerName = playerName,
            errorMessage = errorMessage
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
            Debug.Log($"📤 HOST: Sent ValidationError to {playerName}: {errorMessage}");
        }
    }

    // ==================== UTILITY ====================

    private PlayerState FindPlayerByName(string playerName)
    {
        if (gameManager.hostPlayer != null && gameManager.hostPlayer.playerName == playerName)
            return gameManager.hostPlayer;

        if (gameManager.clientPlayer != null && gameManager.clientPlayer.playerName == playerName)
            return gameManager.clientPlayer;

        return null;
    }
}