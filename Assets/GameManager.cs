using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun; // Make sure this is present if you're using Photon
// using UIManager; // You generally don't need this if UIManager is in global namespace and has Instance
using UnityEngine.UI;
using TMPro;

// Define enums for clarity and type safety
// Keep this definition here as it's used by GameManager AND UIManager now
public enum GamePhase
{
    Lobby,
    RoleAssignment,
    NightMafia,
    NightDoctor,
    NightDetective,
    DayDiscussion,
    DayVoting,
    Elimination,
    GameEnd
}


// Data structure to store info about each player
[System.Serializable]
public class PlayerState
{
    public string id; // id unique for each player, could be a GUID or username
    public string name; // Player's display name
    public string role; // Could be enum or string: "Mafia", "Doctor", etc.
    public bool isAlive = true; // is player still in game?
    public string voteTarget; // who has this player voted for? (id of another player)
    public bool hasVoted = false; // has this player already voted?
}

// these will be stored in a list like players: PlayerState[]

public class GameManager : MonoBehaviourPunCallbacks // Added MonoBehaviourPunCallbacks for Photon events
{
    // game state variables
    public GamePhase currentPhase = GamePhase.Lobby;
    public List<PlayerState> players = new List<PlayerState>();

    // phase timers
    public float phaseTimer = 0f;

    // targets for night actions
    public string mafiaTarget = null;
    public string doctorTarget = null;
    public string detectiveTarget = null;

    // Voting data: maps player ID to number of votes
    public Dictionary<string, int> votes = new Dictionary<string, int>();

    public string playerPrefabName = "Player"; // Must match prefab name in Resources folder

    // Reference to the UIManager (via its singleton instance)
    // private UIManager uiManager; // No need for this, using UIManager.Instance

    // --- Added for testing/initial setup ---
    [Header("Testing/Debugging")]
    public PlayerState myLocalPlayerState; // Manually set your local player's state for testing UI
    public bool isMyPlayerSpawned = false; // Flag to indicate if this is the local player's GameManager instance

    void Start()
    {
        Debug.Log("Game Manager starting...");

        // Ensure UIManager instance exists
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager instance not found! Make sure UIManager GameObject is in scene and script is attached.");
            return;
        }

        // // TEMP: Mock players or wait for actual networked joins
        // // This part will be handled by Photon Network Join
        // if (players.Count >= 5)
        // {
        //     StartGame();
        // }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Only spawn if this is the Master Client or if a character isn't already spawned for the local player.
            // For a robust game, you'd have more sophisticated spawn logic.
            if (PhotonNetwork.IsMasterClient && !isMyPlayerSpawned) // Simple check for Master Client
            {
                 // Create some mock players for testing purposes if not enough are connected
                 // In a real game, this would populate from PhotonNetwork.PlayerList
                 if (players.Count < 3) // Ensure at least a few players for role assignment
                 {
                     players.Add(new PlayerState { id = "id1", name = "Player1", role = "Civilian", isAlive = true });
                     players.Add(new PlayerState { id = "id2", name = "Player2", role = "Civilian", isAlive = true });
                     players.Add(new PlayerState { id = "id3", name = "Player3", role = "Civilian", isAlive = true });
                 }
                StartGame(); // Master client starts the game
            }
            SpawnPlayer();
        } else {
             Debug.LogWarning("Not connected to Photon. UI will not update based on network state.");
             // For testing outside of Photon, you can manually set your state here:
             // Example for testing:
             myLocalPlayerState = new PlayerState { id = "myLocalId", name = "Me", role = "Doctor", isAlive = true };
             players.Add(myLocalPlayerState); // Add myself to the list
             players.Add(new PlayerState { id = "other1", name = "Friend1", role = "Mafia", isAlive = true });
             players.Add(new PlayerState { id = "other2", name = "Friend2", role = "Civilian", isAlive = true });
             players.Add(new PlayerState { id = "other3", name = "Friend3", role = "Detective", isAlive = true });

             // Manually trigger UI update for testing without Photon
             UpdateUIForLocalPlayer();
        }

        Debug.Log("Game Manager initialized with " + players.Count + " players.");
    }

    void SpawnPlayer()
    {
        // This is a placeholder. In a real game, each player would spawn their own character.
        // The UIManager needs to know who the local player is.
        // If UIManager is on the player's character prefab, photonView.IsMine on that UIManager will handle it.
        // If UIManager is a global singleton, you need to tell it who the local player is.
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-2, 2), 0, UnityEngine.Random.Range(-2, 2));
        PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);

        // For testing, set a flag if this GameManager is associated with the local player's character
        // This logic heavily depends on how your GameManager instance is linked to the local player.
        // A common approach is for GameManager to be a singleton, and characters have their own PhotonViews.
        // Then, the GameManager would get the local player's role from their instantiated character.
    }

    void Update()
    {
        // Handle phase countdown during timed phases
        if (currentPhase == GamePhase.DayDiscussion || currentPhase == GamePhase.DayVoting)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer <= 0f)
            {
                NextPhase(); // Move to the next phase automatically
            }
        }
    }

    public void StartGame()
    {
        AssignRoles();
        currentPhase = GamePhase.NightMafia;
        // Broadcast initial phase and roles
        BroadcastPhaseAndRoles();
    }

    // Randomly assigns roles to players
    void AssignRoles()
    {
        List<string> roles = GenerateRoleList(players.Count);
        List<PlayerState> shuffled = players.OrderBy(x => UnityEngine.Random.value).ToList();

        for (int i = 0; i < shuffled.Count; i++)
        {
            shuffled[i].role = roles[i];
            shuffled[i].isAlive = true;
            shuffled[i].voteTarget = null;
            shuffled[i].hasVoted = false;
        }

        Debug.Log("Roles assigned:");
        foreach (var p in shuffled)
        {
            Debug.Log($"{p.name} - {p.role}");
        }
    }

    // Creates a list of roles based on player count (1 Mafia, 1 Doctor, 1 Detective, rest Civilians)
    List<string> GenerateRoleList(int count)
    {
        List<string> roles = new List<string> { "Mafia", "Doctor", "Detective" };
        while (roles.Count < count)
        {
            roles.Add("Civilian");
        }
        return roles;
    }

    // Call this to start the next phase of the game
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case GamePhase.NightMafia:
                currentPhase = GamePhase.NightDoctor;
                break;
            case GamePhase.NightDoctor:
                currentPhase = GamePhase.NightDetective;
                break;
            case GamePhase.NightDetective:
                ResolveNight(); // Evaluate night actions
                currentPhase = GamePhase.DayDiscussion;
                phaseTimer = 45f; // Give players time to discuss
                break;
            case GamePhase.DayDiscussion:
                currentPhase = GamePhase.DayVoting;
                phaseTimer = 30f; // Voting time
                break;
            case GamePhase.DayVoting:
                ResolveVotes();
                CheckWinCondition();
                // If game not over, move to next Night phase
                if (currentPhase != GamePhase.GameEnd) // Check if CheckWinCondition set GameEnd
                {
                    currentPhase = GamePhase.NightMafia;
                }
                break;
            case GamePhase.Elimination: // This phase needs to be handled to move somewhere
                CheckWinCondition();
                if (currentPhase != GamePhase.GameEnd)
                {
                    currentPhase = GamePhase.NightMafia; // Or DayDiscussion, depending on flow
                }
                break;
        }

        BroadcastPhaseAndRoles(); // Notify everyone of the new phase and roles
    }

    // --- IMPORTANT: This is how GameManager tells UIManager to update ---
    void BroadcastPhaseAndRoles()
    {
        Debug.Log($"Game Manager broadcasting Phase: {currentPhase}");

        List<PlayerState> alivePlayersList = players.Where(p => p.isAlive).ToList();

        // Get arrays for RPC
        string[] alivePlayerIds = alivePlayersList.Select(p => p.id).ToArray();
        string[] alivePlayerNames = alivePlayersList.Select(p => p.name).ToArray();
        string[] alivePlayerRoles = alivePlayersList.Select(p => p.role).ToArray();

        // Find the local player's state (this assumes 'myLocalPlayerState' is correctly set up)
        // In a real Photon game, you'd find the local player's role from their Photon.Pun.Player object
        // or a PlayerState associated with PhotonNetwork.LocalPlayer.
        PlayerState localPlayer = players.Find(p => p.id == PhotonNetwork.LocalPlayer.UserId);
        if (localPlayer == null) // Fallback for testing without Photon
        {
             localPlayer = myLocalPlayerState; // Use the manually set local player for testing
        }

        if (localPlayer != null && UIManager.Instance != null)
        {
            // Call the UIManager's RPC method to update UI for all clients
            // This needs to be called on the UIManager script's PhotonView.
            // If UIManager is a singleton, you'd find its PhotonView.
            // For simplicity, let's assume UIManager is on a GameObject with a PhotonView that all clients see.
            // Or, even better, if UIManager is truly a global singleton, you can use RPCs on GameManger's PhotonView
            // and have the UIManager listen to these RPCs, or just call directly if not using RPCs.

            // The UIManager's RPC_UpdateGamePhaseAndRole needs to be called on a PhotonView.
            // If UIManager is on a GameObject, it has its own PhotonView.
            // If GameManager is handling the RPC, it needs to be on GameManager's PhotonView.
            // For now, let's assume GameManager has a PhotonView and calls RPC on its own.
            // And then UIManager (which is a singleton) receives this RPC or updates directly.

            // Option 1: UIManager has its own PhotonView and receives RPC (Recommended if UIManager is on scene object)
            // UIManager.Instance.photonView.RPC("RPC_UpdateGamePhaseAndRole", RpcTarget.All, (int)currentPhase, localPlayer.role, alivePlayerIds, alivePlayerNames, alivePlayerRoles);

            // Option 2: GameManager has a PhotonView and calls UIManager directly based on its role (for local player)
            // This is simpler for local UI updates based on a global state.
            // The isMine parameter will filter the role-specific UI.
            // The `photonView.IsMine` check within UIManager's `UpdateUI` ensures role-specific UI only for the local player.
            UIManager.Instance.UpdateUI(currentPhase, localPlayer.role, true, alivePlayersList);

            // For remote clients, their respective UIManager would be updated by the master client's RPC.
            // You would need a separate RPC from the Master Client to tell all clients to update their UI based on the global state.
            // Example of a Master Client RPC:
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_BroadcastGameUpdate", RpcTarget.All, (int)currentPhase, localPlayer.role, alivePlayerIds, alivePlayerNames, alivePlayerRoles);
            }
        }
    }

    [PunRPC]
    public void RPC_BroadcastGameUpdate(int phaseInt, string localPlayerRole, string[] alivePlayerIds, string[] alivePlayerNames, string[] alivePlayerRoles)
    {
        GamePhase phase = (GamePhase)phaseInt;
        // The localPlayerRole passed here is the role of the Master Client's player.
        // Each client needs to find *their own* role to pass to UIManager.UpdateUI.

        // Reconstruct alivePlayers list
        List<PlayerState> alivePlayers = new List<PlayerState>();
        for (int i = 0; i < alivePlayerIds.Length; i++)
        {
            alivePlayers.Add(new PlayerState
            {
                id = alivePlayerIds[i],
                name = alivePlayerNames[i],
                role = alivePlayerRoles[i],
                isAlive = true
            });
        }

        // Find the local client's own player state from the `players` list (or PhotonNetwork.LocalPlayer.CustomProperties)
        // This is crucial: Each client's UIManager needs *their own* role.
        PlayerState myCurrentState = players.Find(p => p.id == PhotonNetwork.LocalPlayer.UserId);
        string myActualRole = (myCurrentState != null) ? myCurrentState.role : "Civilian"; // Default to civilian if not found

        // Call UIManager to update UI for this specific client
        UIManager.Instance.UpdateUI(phase, myActualRole, true, alivePlayers); // isMine is true because it's for this client's UI
    }


    // Executes night actions (mafia kill, doctor save)
    void ResolveNight()
    {
        if (mafiaTarget != null && mafiaTarget != doctorTarget)
        {
            var targetPlayer = players.Find(p => p.id == mafiaTarget);
            if (targetPlayer != null) targetPlayer.isAlive = false;
            Debug.Log($"{targetPlayer.name} was killed last night.");
        }
        // TODO: Handle detective’s guess result
    }
    // Called when a player votes for another player
    public void RecordVote(string voterId, string targetId)
    {
        var voter = players.Find(p => p.id == voterId);
        if (voter == null || !voter.isAlive || voter.hasVoted) return;

        voter.voteTarget = targetId;
        voter.hasVoted = true;

        if (!votes.ContainsKey(targetId)) votes[targetId] = 0;
        votes[targetId]++;
    }

    // Resolves the vote count and eliminates the voted player
    void ResolveVotes()
    {
        int maxVotes = 0;
        string target = null;

        foreach (var entry in votes)
        {
            if (entry.Value > maxVotes)
            {
                maxVotes = entry.Value;
                target = entry.Key;
            }
        }

        if (target != null)
        {
            var eliminated = players.Find(p => p.id == target);
            if (eliminated != null)
            {
                eliminated.isAlive = false;
                Debug.Log($"{eliminated.name} was eliminated!");
            }
        }

        ResetVotes();
    }
    // Resets vote-related data before the next voting round
    void ResetVotes()
    {
        foreach (var p in players)
        {
            p.hasVoted = false;
            p.voteTarget = null;
        }
        votes.Clear();
    }

    // Checks if the game has been won by either team
    void CheckWinCondition()
    {
        var aliveMafia = players.FindAll(p => p.role == "Mafia" && p.isAlive);
        var aliveCivilians = players.FindAll(p => p.role != "Mafia" && p.isAlive);

        if (aliveMafia.Count == 0)
        {
            EndGame("Civilians");
        }
        else if (aliveMafia.Count >= aliveCivilians.Count)
        {
            EndGame("Mafia");
        }
    }

    // Ends the game and announces the winner
    void EndGame(string winner)
    {
        currentPhase = GamePhase.GameEnd;
        BroadcastPhaseAndRoles(); // Update UI to show GameEnd panel
        Debug.Log($"Game Over! {winner} win.");
        // TODO: Add UI or restart logic here
    }

    // --- Added for testing outside Photon ---
    // This method is for testing UI updates without full Photon logic
    public void UpdateUIForLocalPlayer()
    {
        List<PlayerState> alivePlayers = players.Where(p => p.isAlive).ToList();
        string localRole = (myLocalPlayerState != null) ? myLocalPlayerState.role : "Civilian";
        UIManager.Instance.UpdateUI(currentPhase, localRole, true, alivePlayers);
    }
}