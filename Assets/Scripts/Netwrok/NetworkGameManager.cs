using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance;

    [Header("References")]
    public GameManager gameManager;
    public NetworkManager networkManager;

    [Header("Network State")]
    public bool isGameStarted = false;
    public bool isHost = false;

    private bool waitingForResolution = false;

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

        // Trigger waiting state
        GameEvents.TriggerWaitingForOpponent(true);
        GameEvents.TriggerStatusMessageUpdated($"You submitted {cardCount} cards. Waiting for opponent...");

        // Client sends to host
        if (!isHost)
        {
            PlayCardsMessage msg = new PlayCardsMessage
            {
                playerName = playerName,
                cardIds = localPlayer.playedCardsThisTurn.Select(c => c.ID).ToList()
            };
            SendLocalMessageToServer(NetworkMessageSerializer.Serialize(msg));
            Debug.Log($"CLIENT: Sent PlayCards to host");
        }

        // Host checks for resolution
        if (isHost)
        {
            CheckForTurnResolution();
        }
    }

    private void CheckForTurnResolution()
    {
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

            // Run resolution
            gameManager.EndTurnWithResolution();

            // Send resolution to client
            StartCoroutine(SendResolutionAfterDelay());
        }
    }

    private IEnumerator SendResolutionAfterDelay()
    {
        // Wait for reveal + resolution to complete
        yield return new WaitForSeconds(gameManager.revealDuration + 1f);

        Debug.Log("📤 Sending TurnResolved message to client");
        SendTurnResolvedMessage();

        // Wait a bit then check for next turn or game end
        yield return new WaitForSeconds(1f);

        waitingForResolution = false;

        if (gameManager.CheckForGameEnd())
        {
            Debug.Log("Game is ending...");
            gameManager.EndGame();
        }
        else
        {
            Debug.Log("Starting next turn...");
            gameManager.StartNewTurn();
            SendTurnStartMessage();
        }
    }

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
            default:
                Debug.LogWarning($"Unknown message: {messageType}");
                break;
        }
    }

    public void BeginGameSetup()
    {
        if (NetworkManager.Instance == null ||
            NetworkManager.Instance.runner == null ||
            !NetworkManager.Instance.runner.IsServer)
        {
            return;
        }

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

            Debug.Log("📤 HOST: Game setup message sent");
        }
    }

    private void HandleStartGame(string json)
    {
        Debug.Log("📥 HandleStartGame");
        StartGameMessage msg = NetworkMessageSerializer.Deserialize<StartGameMessage>(json);

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
    }

    private void HandlePlayCards(string json)
    {
        PlayCardsMessage msg = NetworkMessageSerializer.Deserialize<PlayCardsMessage>(json);
        Debug.Log($"📥 HandlePlayCards: {msg.playerName}");

        PlayerState submittingPlayer = FindPlayerByName(msg.playerName);

        if (submittingPlayer == null)
        {
            Debug.LogError($"Unknown player: {msg.playerName}");
            return;
        }

        if (isHost)
        {
            List<Card> clientSelectedCards = submittingPlayer.GetHandCards()
                .Where(c => msg.cardIds.Contains(c.ID))
                .ToList();

            submittingPlayer.ClearSelection();
            foreach (Card card in clientSelectedCards)
            {
                submittingPlayer.SelectCard(card);
            }

            submittingPlayer.SubmitCards();

            GameEvents.TriggerStatusMessageUpdated($"{msg.playerName} submitted!");

            CheckForTurnResolution();
        }
    }

    private void HandleTurnStart(string json)
    {
        TurnStartMessage msg = NetworkMessageSerializer.Deserialize<TurnStartMessage>(json);
        Debug.Log($"📥 CLIENT: TurnStart - Turn {msg.turnNumber}");

        if (!isHost)
        {
            gameManager.currentTurn = msg.turnNumber;

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

                // Sync energy
                gameManager.hostPlayer.energy = msg.hostEnergy;
                gameManager.clientPlayer.energy = msg.clientEnergy;

                if (localPlayer == gameManager.hostPlayer)
                {
                    GameEvents.TriggerEnergyChanged(msg.hostEnergy, localPlayer.maxEnergy, localPlayer.playerName);
                }
                else
                {
                    GameEvents.TriggerEnergyChanged(msg.clientEnergy, localPlayer.maxEnergy, localPlayer.playerName);
                }
            }

            // Set phase and trigger events
            gameManager.SetPhase(GamePhase.CardSelection);
            GameEvents.TriggerTurnStart(msg.turnNumber);
            GameEvents.TriggerCardSelectionStart();
            GameEvents.TriggerWaitingForOpponent(false);

            // Start timer
            gameManager.turnManager.StartTimer();

            Debug.Log($"CLIENT: Turn {msg.turnNumber} started. Energy - Host: {msg.hostEnergy}, Client: {msg.clientEnergy}");
        }
    }

    private void HandleTurnResolved(string json)
    {
        TurnResolvedMessage msg = NetworkMessageSerializer.Deserialize<TurnResolvedMessage>(json);
        Debug.Log($"📥 CLIENT: TurnResolved - Host: {msg.hostScore}, Client: {msg.clientScore}");

        if (!isHost)
        {
            // Show reveal first
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

            // Update scores after reveal
            StartCoroutine(UpdateScoresAfterReveal(msg));
        }
    }

    private IEnumerator UpdateScoresAfterReveal(TurnResolvedMessage msg)
    {
        yield return new WaitForSeconds(gameManager.revealDuration);

        // Update scores
        gameManager.hostPlayer.score = msg.hostScore;
        gameManager.clientPlayer.score = msg.clientScore;

        PlayerState local = gameManager.GetLocalPlayer();
        PlayerState opponent = gameManager.GetOpponentPlayer();

        Debug.Log($"CLIENT: Updating scores - Host: {msg.hostScore}, Client: {msg.clientScore}");

        if (local != null)
        {
            GameEvents.TriggerScoreChanged(local.score, local.playerName);
        }
        if (opponent != null)
        {
            GameEvents.TriggerScoreChanged(opponent.score, opponent.playerName);
        }

        gameManager.SetPhase(GamePhase.TurnEnd);
        GameEvents.TriggerTurnEnd();
        GameEvents.TriggerWaitingForOpponent(false);
    }

    private void HandleGameEnd(string json)
    {
        GameEndMessage msg = NetworkMessageSerializer.Deserialize<GameEndMessage>(json);
        Debug.Log($"📥 GameEnd: Winner={msg.winner}, Host={msg.hostScore}, Client={msg.clientScore}");

        gameManager.SetPhase(GamePhase.GameOver);

        // Update final scores
        gameManager.hostPlayer.score = msg.hostScore;
        gameManager.clientPlayer.score = msg.clientScore;

        GameEvents.TriggerScoreChanged(gameManager.hostPlayer.score, gameManager.hostPlayer.playerName);
        GameEvents.TriggerScoreChanged(gameManager.clientPlayer.score, gameManager.clientPlayer.playerName);

        GameEvents.TriggerGameEnd(msg.winner, msg.hostScore, msg.clientScore);
    }

    public void SendLocalMessageToServer(string json)
    {
        if (isHost) return;

        NetworkPlayer localNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (localNetPlayer != null)
        {
            localNetPlayer.RPC_SendToServer(json);
        }
    }

    public void SendTurnStartMessage()
    {
        if (!isHost) return;

        TurnStartMessage msg = new TurnStartMessage
        {
            turnNumber = gameManager.currentTurn,
            hostEnergy = gameManager.hostPlayer.energy,
            clientEnergy = gameManager.clientPlayer.energy
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
            Debug.Log($"📤 HOST: Sent TurnStart - Turn {msg.turnNumber}");
        }
    }

    public void SendTurnResolvedMessage()
    {
        if (!isHost) return;

        List<int> hostCards = gameManager.hostPlayer.playedCardsThisTurn.Select(c => c.ID).ToList();
        List<int> clientCards = gameManager.clientPlayer.playedCardsThisTurn.Select(c => c.ID).ToList();

        TurnResolvedMessage msg = new TurnResolvedMessage
        {
            hostScore = gameManager.hostPlayer.score,
            clientScore = gameManager.clientPlayer.score,
            hostPlayedCards = hostCards,
            clientPlayedCards = clientCards
        };

        string json = NetworkMessageSerializer.Serialize(msg);
        NetworkPlayer hostNetPlayer = networkManager.GetLocalNetworkPlayer();
        if (hostNetPlayer != null)
        {
            hostNetPlayer.SendGameMessageAsHost(json);
            Debug.Log($"📤 HOST: Sent TurnResolved - Scores: {msg.hostScore}/{msg.clientScore}");
        }
    }

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

    private PlayerState FindPlayerByName(string playerName)
    {
        if (gameManager.hostPlayer != null && gameManager.hostPlayer.playerName == playerName)
            return gameManager.hostPlayer;

        if (gameManager.clientPlayer != null && gameManager.clientPlayer.playerName == playerName)
            return gameManager.clientPlayer;

        return null;
    }
}