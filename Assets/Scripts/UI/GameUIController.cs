using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance;

    [Header("Panels")]
    public GameObject lobbyUI;
    public GameObject gameUI;

    [Header("Card Reveal UI")]
    public CardUI cardUIPrefab;
    public GameObject revealPanel;
    public RectTransform hostPlayedCardsContainer;
    public RectTransform clientPlayedCardsContainer;
    public TMP_Text revealTitleText; // NEW: Shows whose cards are whose

    [Header("Text UI")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text opponentScoreText; // NEW: Opponent score display
    public TMP_Text energyText;
    public TMP_Text turnInfoText;
    public TMP_Text statusText;
    public TMP_Text opponentStatusText;
    public Button submitButton;

    private PlayerState localPlayer;
    private PlayerState opponentPlayer;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(PlayerState player)
    {
        localPlayer = player;
        opponentPlayer = GameManager.Instance.GetOpponentPlayer();

        UpdateScore(player.score);
        UpdateOpponentScore(opponentPlayer != null ? opponentPlayer.score : 0);
        UpdateEnergy(player.energy, player.maxEnergy);
        UpdateTurnInfo(1);
        ShowGameUI();

        // Subscribe to events
        GameEvents.OnTurnTimerUpdated += OnTimerUpdate;
        GameEvents.OnScoreChanged += OnScoreChanged;
        GameEvents.OnEnergyChanged += OnEnergyChanged;
        GameEvents.OnTurnStart += OnTurnStarted;
        GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
        GameEvents.OnCardsRevealed += OnCardsRevealed;
        GameEvents.OnWaitingForOpponent += OnWaitingForOpponent;
        GameEvents.OnStatusMessageUpdated += OnStatusMessageUpdated;
        GameEvents.OnPlayerSubmittedCards += OnPlayerSubmittedCards;
        GameEvents.OnUIUpdateRequested += OnUIUpdateRequested;
        GameEvents.OnGameEnd += OnGameEnd;

        if (revealPanel != null)
        {
            revealPanel.SetActive(false);
        }

        UpdateStatusText("Waiting for game to start...");
        UpdateOpponentStatus("Opponent: Connecting...");

        Debug.Log($"UI Initialized for {player.playerName}");
    }

    private void OnDestroy()
    {
        GameEvents.OnTurnTimerUpdated -= OnTimerUpdate;
        GameEvents.OnScoreChanged -= OnScoreChanged;
        GameEvents.OnEnergyChanged -= OnEnergyChanged;
        GameEvents.OnTurnStart -= OnTurnStarted;
        GameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
        GameEvents.OnGameInitialized -= ShowGameUI;
        GameEvents.OnCardsRevealed -= OnCardsRevealed;
        GameEvents.OnWaitingForOpponent -= OnWaitingForOpponent;
        GameEvents.OnStatusMessageUpdated -= OnStatusMessageUpdated;
        GameEvents.OnPlayerSubmittedCards -= OnPlayerSubmittedCards;
        GameEvents.OnUIUpdateRequested -= OnUIUpdateRequested;
        GameEvents.OnGameEnd -= OnGameEnd;
    }

    private void Start()
    {
        ShowLobbyUI();
        submitButton.gameObject.SetActive(false);
        submitButton.onClick.AddListener(OnSubmit);
        GameEvents.OnCardSelectionStart += ShowSubmitButton;
        GameEvents.OnTurnResolved += HideSubmitButton;
        GameEvents.OnGameInitialized += ShowGameUI;
    }

    private void ShowSubmitButton()
    {
        submitButton.gameObject.SetActive(true);
        submitButton.interactable = true;
        UpdateStatusText("Select cards and click Submit!");
    }

    private void HideSubmitButton()
    {
        submitButton.gameObject.SetActive(false);
    }

    public void OnSubmit()
    {
        if (localPlayer == null) return;

        if (localPlayer.GetSelectedCards().Count == 0)
        {
            UpdateStatusText("⚠️ No cards selected! (You can pass)");
            // Allow empty submission to pass turn
        }

        int totalCost = localPlayer.GetSelectedCardsCost();
        if (totalCost > localPlayer.energy)
        {
            UpdateStatusText($"⚠️ Not enough energy! Need: {totalCost}, Have: {localPlayer.energy}");
            return;
        }

        PlayerRef local = NetworkManager.Instance.runner.LocalPlayer;
        GameManager.Instance.SubmitPlayerCards(local);

        submitButton.interactable = false;
        UpdateStatusText("✅ Submitted! Waiting for opponent...");
    }

    private void OnTimerUpdate(float time)
    {
        int sec = Mathf.FloorToInt(time);
        timerText.text = $"⏱️ {sec}s";

        if (sec <= 10)
        {
            timerText.color = Color.red;
        }
        else if (sec <= 20)
        {
            timerText.color = Color.yellow;
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    private void OnScoreChanged(int newScore, string playerName)
    {
        Debug.Log($"OnScoreChanged: {playerName} = {newScore}");

        if (localPlayer != null && localPlayer.playerName == playerName)
        {
            UpdateScore(newScore);
        }
        else if (opponentPlayer != null && opponentPlayer.playerName == playerName)
        {
            UpdateOpponentScore(newScore);
        }
    }

    private void OnEnergyChanged(int current, int max, string playerName)
    {
        if (localPlayer != null && localPlayer.playerName == playerName)
        {
            UpdateEnergy(current, max);
        }
    }

    private void OnGamePhaseChanged(GamePhase phase)
    {
        Debug.Log($"UI Phase Changed: {phase.GetPhaseName()}");

        turnInfoText.text = phase.GetPhaseName();

        switch (phase)
        {
            case GamePhase.CardSelection:
                UpdateStatusText("🎴 Your turn! Select cards to play.");
                UpdateOpponentStatus("Opponent: Selecting cards...");
                break;
            case GamePhase.CardReveal:
                UpdateStatusText("👀 Revealing cards...");
                break;
            case GamePhase.Resolution:
                UpdateStatusText("⚔️ Resolving abilities...");
                break;
            case GamePhase.TurnEnd:
                UpdateStatusText("✅ Turn complete!");
                break;
            case GamePhase.GameOver:
                UpdateStatusText("🏁 Game Over!");
                break;
        }
    }

    private void OnWaitingForOpponent(bool isWaiting)
    {
        if (isWaiting)
        {
            UpdateStatusText("⏳ Waiting for opponent...");
            UpdateOpponentStatus("Opponent: Still selecting...");
        }
        else
        {
            UpdateStatusText("✅ Both players ready!");
            UpdateOpponentStatus("Opponent: Ready!");
        }
    }

    private void OnStatusMessageUpdated(string message)
    {
        UpdateStatusText(message);
    }

    private void OnPlayerSubmittedCards(string playerName, int cardCount)
    {
        if (opponentPlayer != null && opponentPlayer.playerName == playerName)
        {
            UpdateOpponentStatus($"Opponent: ✅ Submitted {cardCount} card(s)");
        }
    }

    private void OnUIUpdateRequested()
    {
        if (localPlayer != null && GameManager.Instance.currentPhase == GamePhase.CardSelection)
        {
            int selectedCount = localPlayer.GetSelectedCards().Count;
            int totalCost = localPlayer.GetSelectedCardsCost();
            bool canAfford = totalCost <= localPlayer.energy;

            submitButton.interactable = canAfford && !localPlayer.hasSubmittedCards;

            if (selectedCount > 0)
            {
                if (!canAfford)
                {
                    UpdateStatusText($"⚠️ Cost: {totalCost} | Energy: {localPlayer.energy} - TOO EXPENSIVE!");
                }
                else
                {
                    UpdateStatusText($"✅ Selected: {selectedCount} | Cost: {totalCost}/{localPlayer.energy}");
                }
            }
        }
    }

    private void OnGameEnd(string winner, int hostScore, int clientScore)
    {
        Debug.Log($"UI: Game ended. Winner: {winner}");

        // Update final scores
        UpdateScore(localPlayer.score);
        UpdateOpponentScore(opponentPlayer.score);

        string endMessage = "";
        if (winner == "Tie")
        {
            endMessage = $"🤝 TIE GAME! Final Score: {localPlayer.score} - {opponentPlayer.score}";
        }
        else if (winner == localPlayer.playerName)
        {
            endMessage = $"🏆 YOU WIN! {localPlayer.score} - {opponentPlayer.score}";
        }
        else
        {
            endMessage = $"😢 YOU LOSE! {localPlayer.score} - {opponentPlayer.score}";
        }

        UpdateStatusText(endMessage);

        // Hide reveal panel if visible
        if (revealPanel != null)
        {
            revealPanel.SetActive(false);
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Your Score: {score}";
        }
    }

    public void UpdateOpponentScore(int score)
    {
        if (opponentScoreText != null)
        {
            opponentScoreText.text = $"Opponent: {score}";
        }
    }

    public void UpdateEnergy(int current, int max)
    {
        if (energyText != null)
        {
            energyText.text = $"⚡ Energy: {current}/{max}";
        }
    }

    public void UpdateTurnInfo(int turn)
    {
        if (turnInfoText != null)
        {
            turnInfoText.text = $"Turn {turn} / {GameManager.Instance.maxTurns}";
        }
    }

    public void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void UpdateOpponentStatus(string message)
    {
        if (opponentStatusText != null)
        {
            opponentStatusText.text = message;
        }
    }

    public void ShowGameUI()
    {
        lobbyUI.SetActive(false);
        gameUI.SetActive(true);
    }

    public void ShowLobbyUI()
    {
        gameUI.SetActive(false);
        lobbyUI.SetActive(true);
    }

    private void OnCardsRevealed(List<CardData> hostCards, List<CardData> clientCards)
    {
        Debug.Log($"OnCardsRevealed - Host: {hostCards.Count}, Client: {clientCards.Count}");

        if (revealPanel != null)
        {
            revealPanel.SetActive(true);
        }

        submitButton.gameObject.SetActive(false);

        bool isLocalPlayerHost = localPlayer == GameManager.Instance.hostPlayer;

        RectTransform localContainer = isLocalPlayerHost ? hostPlayedCardsContainer : clientPlayedCardsContainer;
        RectTransform opponentContainer = isLocalPlayerHost ? clientPlayedCardsContainer : hostPlayedCardsContainer;

        List<CardData> localCardData = isLocalPlayerHost ? hostCards : clientCards;
        List<CardData> opponentCardData = isLocalPlayerHost ? clientCards : hostCards;

        DisplayPlayedCards(localContainer, localCardData, "YOU");
        DisplayPlayedCards(opponentContainer, opponentCardData, "OPPONENT");

        if (revealTitleText != null)
        {
            revealTitleText.text = "🎴 CARDS REVEALED! 🎴";
        }

        UpdateStatusText("👀 Cards revealed! Calculating scores...");
    }

    private void DisplayPlayedCards(RectTransform container, List<CardData> cardDataList, string label)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (cardUIPrefab == null)
        {
            Debug.LogError("CardUI Prefab not assigned!");
            return;
        }

        if (cardDataList.Count == 0)
        {
            // Show "No cards played" message
            GameObject emptyMsg = new GameObject("EmptyMessage");
            emptyMsg.transform.SetParent(container);
            TMP_Text text = emptyMsg.AddComponent<TextMeshProUGUI>();
            text.text = $"{label}: No cards played";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18;
            return;
        }

        foreach (CardData data in cardDataList)
        {
            CardUI cardUI = Instantiate(cardUIPrefab, container);

            cardUI.nameText.text = data.name;
            cardUI.costText.text = $"⚡{data.cost}";
            cardUI.powerText.text = $"⚔️{data.power}";

            string abilitiesStr = data.abilities != null && data.abilities.Count > 0
                ? string.Join("\n", data.abilities)
                : "None";

            cardUI.abilitiesText.text = abilitiesStr;

            if (cardUI.cardButton != null)
            {
                cardUI.cardButton.interactable = false;
            }
            cardUI.UpdateSelectionVisual(false);
            cardUI.gameObject.name = $"Revealed_{data.name}_{label}";
        }
    }

    private void OnTurnStarted(int turnNumber)
    {
        Debug.Log($"UI: Turn {turnNumber} started");

        UpdateTurnInfo(turnNumber);

        if (revealPanel != null)
        {
            revealPanel.SetActive(false);
        }

        UpdateStatusText($"🎮 Turn {turnNumber} started!");
        UpdateOpponentStatus("Opponent: Selecting cards...");

        // Update scores
        if (localPlayer != null)
        {
            UpdateScore(localPlayer.score);
        }
        if (opponentPlayer != null)
        {
            UpdateOpponentScore(opponentPlayer.score);
        }
    }
}