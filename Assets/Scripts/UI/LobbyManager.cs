using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI")]
    public Button hostButton;
    public Button joinButton;
    public Button quickJoinButton;
    
    public TMP_InputField roomInput;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playerListText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        hostButton.onClick.AddListener(() => NetworkManager.Instance.HostSession());
        joinButton.onClick.AddListener(() => NetworkManager.Instance.JoinSession(roomInput.text));
        quickJoinButton.onClick.AddListener(() => NetworkManager.Instance.QuickJoin());

        NetworkManager.OnStatusUpdated += UpdateStatus;
        NetworkManager.OnPlayerListUpdated += UpdatePlayerList;
    }

    public void SetRoomInput(string room)
    {
        roomInput.text = room;
    }

    private void UpdateStatus(string status)
    {
        statusText.text = status;
    }

    private void UpdatePlayerList(List<NetworkPlayer> players)
    {
        string newText = "";

        if (players == null || players.Count == 0)
        {
            newText = "No players yet...";
        }
        else
        {
            foreach (var p in players)
            {
                if (p == null) continue;

                // Get the player name - handle empty names
                string name = p.PlayerName.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    name = "Player (loading...)";
                }

                string line = name;
                if (p.IsHost) line += " (Host)";
                if (p.IsLocalPlayer) line += " (You)";

                newText += line + "\n";
            }
        }

        // Only update UI and log if the text actually changed
        if (playerListText.text != newText)
        {
            playerListText.text = newText;
            Debug.Log($"Player list UI updated: {players?.Count ?? 0} player(s)");
        }
    }

    private void OnDestroy()
    {
        NetworkManager.OnStatusUpdated -= UpdateStatus;
        NetworkManager.OnPlayerListUpdated -= UpdatePlayerList;
    }
}