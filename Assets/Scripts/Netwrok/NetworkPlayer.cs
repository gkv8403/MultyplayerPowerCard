using Fusion;
using TMPro;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public NetworkBool IsHost { get; set; }

    // Local flag for checking if this is the local player
    public bool IsLocalPlayer => Object != null && Object.HasInputAuthority;

    // Optional: display name text (uncomment if you want to show it on the player)
    // public TextMeshProUGUI displayNameText;

    private NetworkString<_16> lastPlayerName;
    private bool lastIsHost;
    private bool hasNotifiedSpawn = false;

    public override void Spawned()
    {
        base.Spawned();

        Debug.Log($"NetworkPlayer spawned. PlayerName: {PlayerName}, IsHost: {IsHost}, HasInputAuthority: {Object.HasInputAuthority}");

        // Store initial values
        lastPlayerName = PlayerName;
        lastIsHost = IsHost;
        hasNotifiedSpawn = false;

        // Update display
        UpdateDisplayName();

        // If player already has a name (host), notify immediately
        if (!string.IsNullOrEmpty(PlayerName.ToString()))
        {
            hasNotifiedSpawn = true;
            NotifyPlayerListUpdate();
        }
    }

    private void NotifyPlayerListUpdate()
    {
        // Trigger a player list refresh via NetworkManager
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RefreshPlayerList();
        }
    }

    // Called when this object is about to be destroyed
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        Debug.Log($"NetworkPlayer despawned: {PlayerName}");

        // Notify that a player left
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RefreshPlayerList();
        }
    }

    // Called every network tick - check for changes and update UI
    public override void FixedUpdateNetwork()
    {
        // Check if networked values have changed
        if (!PlayerName.Equals(lastPlayerName) || IsHost != lastIsHost)
        {
            bool wasEmpty = string.IsNullOrEmpty(lastPlayerName.ToString());
            bool isNowFilled = !string.IsNullOrEmpty(PlayerName.ToString());

            lastPlayerName = PlayerName;
            lastIsHost = IsHost;
            UpdateDisplayName();

            // Only notify when:
            // 1. Name changes from empty to filled (initial sync for clients)
            // 2. Name changes from one value to another
            // 3. Host status changes
            if ((wasEmpty && isNowFilled) || (!wasEmpty && isNowFilled))
            {
                Debug.Log($"Player data changed: {PlayerName}, IsHost: {IsHost}");

                if (!hasNotifiedSpawn)
                {
                    hasNotifiedSpawn = true;
                }

                NotifyPlayerListUpdate();
            }
        }
    }

    private void UpdateDisplayName()
    {
        string name = PlayerName.ToString();

        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("PlayerName is empty, skipping update");
            return;
        }

        string displayName = name;
        if (IsHost) displayName += " (Host)";
        if (IsLocalPlayer) displayName += " (You)";

        // Uncomment if you have a display text component
        // if (displayNameText != null) displayNameText.text = displayName;

        // For debugging
        gameObject.name = displayName;

        Debug.Log($"Display name updated to: {displayName}");
    }

    // Optional: Use this if you need to force a UI refresh
    public void ForceUpdateDisplay()
    {
        UpdateDisplayName();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SendToServer(string json, RpcInfo info = default)
    {
        Debug.Log($"[RPC] Received from client ({Object.InputAuthority}): {json}");

        // Only the server should process and broadcast
        if (Runner.IsServer)
        {
            // Let the NetworkGameManager handle the message (server-side)
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ReceiveMessage(json);
            }

            // Broadcast to all clients (include sender if you want)
            RPC_BroadcastToClients(json);
        }
    }

    // Server -> All Clients broadcast (StateAuthority -> All)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastToClients(string json, RpcInfo info = default)
    {
        Debug.Log($"[RPC] Broadcast to client ({(Object.HasInputAuthority ? "Local" : "Remote")}): {json}");

        // On every client this will be executed
        if (!Runner.IsServer)
        {
            // Forward to NetworkGameManager on client so it can react
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ReceiveMessage(json);
            }
        }
    }

    // Helper for a local host to directly broadcast a message via its own RPC
    public void SendGameMessageAsHost(string json)
    {
        // If this instance has StateAuthority (host), call broadcast
        if (Object != null && Object.HasStateAuthority)
        {
            RPC_BroadcastToClients(json);
            // Also process locally
            if (NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.ReceiveMessage(json);
        }
        else
        {
            // If this client wants to send, call RPC_SendToServer
            if (Object != null && Object.HasInputAuthority)
            {
                RPC_SendToServer(json);
            }
        }
    }
}