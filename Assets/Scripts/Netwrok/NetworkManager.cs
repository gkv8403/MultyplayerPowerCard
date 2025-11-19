using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance;

    [Header("Runner")]
    public NetworkRunner runnerPrefab;
    public NetworkRunner runner;

    [Header("Player Prefab")]
    public NetworkPlayer playerPrefab;

    public static event Action<string> OnStatusUpdated;
    public static event Action<List<NetworkPlayer>> OnPlayerListUpdated;

    private Dictionary<PlayerRef, NetworkPlayer> connectedPlayers = new Dictionary<PlayerRef, NetworkPlayer>();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
    }

    // -------------------------------
    // Host
    // -------------------------------
    public async void HostSession()
    {
        UpdateStatus("Hosting...");
        runner = Instantiate(runnerPrefab);
        runner.ProvideInput = true;

        runner.AddCallbacks(this);

        string roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            LobbyManager.Instance.SetRoomInput(roomName);
            UpdateStatus("Room Created: " + roomName);
        }
        else
        {
            UpdateStatus("Failed to create room: " + result.ErrorMessage);
        }
    }

    // -------------------------------
    // Join
    // -------------------------------
    public async void JoinSession(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("Room ID required!");
            return;
        }

        UpdateStatus("Joining...");
        runner = Instantiate(runnerPrefab);
        runner.ProvideInput = true;

        runner.AddCallbacks(this);

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            UpdateStatus("You joined " + roomName + " successfully!");
        }
        else
        {
            UpdateStatus("Failed to join: " + result.ErrorMessage);
        }
    }

    // -------------------------------
    // Quick Join
    // -------------------------------
    public async void QuickJoin()
    {
        UpdateStatus("Quick joining...");
        runner = Instantiate(runnerPrefab);
        runner.ProvideInput = true;

        runner.AddCallbacks(this);

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = null,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            UpdateStatus("Joined/Created room successfully!");
        }
        else
        {
            UpdateStatus("Failed to quick join: " + result.ErrorMessage);
        }
    }

    // -------------------------------
    // Player Joined
    // -------------------------------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined called for {player}");

        // Only the host/server spawns player objects
        if (!runner.IsServer)
        {
            Debug.Log("Not server, skipping spawn");
            return;
        }

        // Prevent > 2 players
        if (connectedPlayers.Count >= 2)
        {
            UpdateStatus("Room FULL! Cannot join.");
            runner.Disconnect(player);
            return;
        }

        // Generate random name on the server
        string randomName = "Player" + UnityEngine.Random.Range(100, 999);
        bool isHost = (player == runner.LocalPlayer);

        Debug.Log($"Spawning player: {randomName}, IsHost: {isHost}");

        // Spawn network player with InputAuthority set to the joining player
        var newPlayer = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);

        // Set the networked properties directly (since we're the server/state authority)
        newPlayer.PlayerName = randomName;
        newPlayer.IsHost = isHost;

        connectedPlayers[player] = newPlayer;

        UpdateStatus($"Player {randomName} joined!");
        UpdatePlayerListPanel();
        // NEW CODE: Check if we have 2 players to start the game
        if (runner.IsServer && connectedPlayers.Count == 2)
        {
            Debug.Log("2 Players Connected! Starting Game Sequence...");
            // Add a small delay to ensure the client is fully ready
            StartCoroutine(StartGameRoutine());
        }

    }
    private System.Collections.IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(1.0f); // Wait for connection stability

        if (NetworkGameManager.Instance != null)
        {
            print(1);
            NetworkGameManager.Instance.BeginGameSetup();
        }
    }
    // -------------------------------
    // Player Left
    // -------------------------------
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft called for {player}");

        if (connectedPlayers.ContainsKey(player))
        {
            if (connectedPlayers[player] != null && connectedPlayers[player].Object != null)
            {
                runner.Despawn(connectedPlayers[player].Object);
            }
            connectedPlayers.Remove(player);
            UpdatePlayerListPanel();
        }
    }

    // -------------------------------
    // UI Update Helpers
    // -------------------------------
    private void UpdatePlayerListPanel()
    {
        // Immediate update - no delay needed for event-driven updates
        UpdatePlayerListNow();
    }

    private void UpdatePlayerListNow()
    {
        List<NetworkPlayer> list = new List<NetworkPlayer>();

        // Find all NetworkPlayer objects that are spawned
        if (runner != null)
        {
            NetworkPlayer[] allPlayers = FindObjectsOfType<NetworkPlayer>();

            // Filter out players with empty names (still loading)
            foreach (var player in allPlayers)
            {
                if (player != null && !string.IsNullOrEmpty(player.PlayerName.ToString()))
                {
                    list.Add(player);
                }
            }

            Debug.Log($"Player list updated: {list.Count} player(s) ready");
        }

        OnPlayerListUpdated?.Invoke(list);
    }
    // Inside NetworkManager.cs

    // ... (after OnHostMigration or in a UTILITY section)

    /// <summary>
    /// Get the NetworkPlayer object that belongs to the local user.
    /// </summary>
    public NetworkPlayer GetLocalNetworkPlayer()
    {
        if (runner == null || !runner.IsConnectedToServer) return null;

        // The LocalPlayer is the PlayerRef (ID) of the current machine's player.
        PlayerRef localRef = runner.LocalPlayer;

        if (connectedPlayers.TryGetValue(localRef, out NetworkPlayer localNetPlayer))
        {
            return localNetPlayer;
        }

        return null;
    }
    private void UpdateStatus(string status)
    {
        Debug.Log(status);
        OnStatusUpdated?.Invoke(status);
    }

    // Public method to allow external classes to trigger player list updates
    public void RefreshPlayerList()
    {
        UpdatePlayerListPanel();
    }

    // Public method to disconnect from the session
    public void LeaveSession()
    {
        if (runner != null)
        {
            Debug.Log("Leaving session...");
            runner.Shutdown();
            UpdateStatus("Disconnected");
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server");
        // Don't update here - wait for player spawns
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
        UpdatePlayerListPanel();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        UpdateStatus($"Connection failed: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Shutdown: {shutdownReason}");
        connectedPlayers.Clear();
        UpdatePlayerListPanel();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef playerRef, NetworkInput input) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
}