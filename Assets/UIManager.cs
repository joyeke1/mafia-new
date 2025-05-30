using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Button, Text
using Photon.Pun; // Required for PhotonView and IsMine property
using System.Collections.Generic; // For List<PlayerState>
using System.Linq; // For .ToList() and other LINQ operations
using System; // For Action delegate
using TMPro;

// IMPORTANT: GamePhase enum is now assumed to be defined elsewhere (e.g., in GameManager.cs)
// public enum GamePhase { ... } // REMOVED FROM HERE
// public enum PlayerRole { ... } // REMOVED FROM HERE, using string for role instead

public class UIManager : MonoBehaviourPunCallbacks // Inherit from MonoBehaviourPunCallbacks for Photon events
{
    // --- UI Panel References ---
    // Assign these GameObjects (which are your UI Panels) in the Unity Inspector
    [Header("Role-Specific UI Panels")]
    public GameObject mafiaPanel;       // Contains kill button, player list for Mafia
    public GameObject doctorPanel;      // Contains save button for Doctor
    public GameObject detectivePanel;   // Contains investigation menu for Detective
    public GameObject civilianPanel;    // General UI for Civilians (e.g., just chat during night)

    [Header("Phase-Specific UI Panels")]
    public GameObject chatPanel;        // Visible during discussion phase, maybe always
    public GameObject votingPanel;      // Visible during voting phase
    public GameObject gameEndPanel;     // Visible when the game ends
    public GameObject lobbyPanel;       // Visible in the lobby

    // --- Internal State Variables ---
    private GamePhase currentPhase; // Now uses the GamePhase enum defined in GameManager.cs
    private string myRole;          // Now uses string for role, matching PlayerState
    private bool isLocalPlayer;     // Derived from photonView.IsMine

    // --- Example UI Elements (for demonstration, link in Inspector) ---
    [Header("Example UI Elements (for demonstration)")]
    public Button mafiaKillButton;
    public Text mafiaPlayerListText; // For Mafia's simple text list (or could use dynamic buttons)

    public Button doctorSaveButton;
    public Button detectiveInvestigateButton;
    public Text chatWindowText; // Example of a chat window
    public Button voteConfirmButton; // Example of a voting button

    // --- Dynamic Player List Elements ---
    [Header("Dynamic Player List UI Elements")]
    public GameObject playerListItemPrefab; // Assign your PlayerListItemButton prefab here
    public Transform doctorPlayerListContent; // Assign the Content Transform of Doctor's Scroll View
    public Transform detectivePlayerListContent; // Assign the Content Transform of Detective's Scroll View
    public Transform votingPlayerListContent; // Assign the Content Transform of Voting Scroll View

    private string selectedPlayerIdForAction; // Stores the ID of the player currently selected for an action

    // --- Singleton Pattern (Optional but recommended for UI Managers) ---
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Ensure all panels are initially inactive
        DeactivateAllPanels();
    }

    // This method should be called whenever the game phase or player role changes.
    // In a real game, this would likely be called by your Game Manager script
    // and synchronized across the network.
    public void UpdateUI(GamePhase newPhase, string newRole, bool isMine, List<PlayerState> alivePlayers = null)
    {
        currentPhase = newPhase;
        myRole = newRole;
        isLocalPlayer = isMine; // This tells us if this UI should react to role-specific actions

        Debug.Log($"Updating UI: Phase={currentPhase}, Role={myRole}, IsLocalPlayer={isLocalPlayer}");

        // 1. Always start by deactivating all panels to ensure a clean state.
        DeactivateAllPanels();

        // Clear any previous selections
        selectedPlayerIdForAction = null;

        // 2. Logic based on the current game phase
        switch (currentPhase)
        {
            case GamePhase.Lobby:
                lobbyPanel.SetActive(true);
                // Maybe show a "waiting for players" message
                break;

            case GamePhase.NightMafia:
                if (isLocalPlayer)
                {
                    if (myRole == "Mafia")
                    {
                        mafiaPanel.SetActive(true);
                        // For Mafia, let's use the simple text list for now, but could be dynamic too
                        if (mafiaPlayerListText != null && alivePlayers != null)
                        {
                            mafiaPlayerListText.text = "Alive Players:\n" + string.Join("\n", alivePlayers.Select(p => p.name));
                        }
                        // If you wanted dynamic buttons for Mafia, you'd call PopulatePlayerSelectionList here
                    }
                    else if (myRole == "Civilian")
                    {
                        civilianPanel.SetActive(true); // E.g., shows "Night. Waiting for actions."
                    }
                }
                break;

            case GamePhase.NightDoctor:
                if (isLocalPlayer)
                {
                    if (myRole == "Doctor")
                    {
                        doctorPanel.SetActive(true);
                        // Populate the doctor's player selection list
                        if (doctorPlayerListContent != null && playerListItemPrefab != null && alivePlayers != null)
                        {
                            PopulatePlayerSelectionList(doctorPlayerListContent, alivePlayers, (selectedId) =>
                            {
                                selectedPlayerIdForAction = selectedId;
                                Debug.Log($"Doctor selected: {selectedId}");
                                // You might visually highlight the selected player here
                            });
                        }
                    }
                    else if (myRole == "Civilian")
                    {
                        civilianPanel.SetActive(true);
                    }
                }
                break;

            case GamePhase.NightDetective:
                if (isLocalPlayer)
                {
                    if (myRole == "Detective")
                    {
                        detectivePanel.SetActive(true);
                        // Populate the detective's player selection list
                        if (detectivePlayerListContent != null && playerListItemPrefab != null && alivePlayers != null)
                        {
                            PopulatePlayerSelectionList(detectivePlayerListContent, alivePlayers, (selectedId) =>
                            {
                                selectedPlayerIdForAction = selectedId;
                                Debug.Log($"Detective selected: {selectedId}");
                                // You might visually highlight the selected player here
                            });
                        }
                    }
                    else if (myRole == "Civilian")
                    {
                        civilianPanel.SetActive(true);
                    }
                }
                break;

            case GamePhase.DayDiscussion:
                chatPanel.SetActive(true); // Chat is active for everyone during the day
                civilianPanel.SetActive(true); // General day UI for all
                // Potentially other general day-time UIs
                break;

            case GamePhase.DayVoting:
                chatPanel.SetActive(true); // Chat might still be active
                votingPanel.SetActive(true); // Voting UI for everyone
                // Populate voting options
                if (votingPlayerListContent != null && playerListItemPrefab != null && alivePlayers != null)
                {
                     PopulatePlayerSelectionList(votingPlayerListContent, alivePlayers, (selectedId) =>
                     {
                         selectedPlayerIdForAction = selectedId;
                         Debug.Log($"Voted for: {selectedId}");
                         // You might visually highlight the selected player here
                     });
                }
                break;

            case GamePhase.GameEnd:
                gameEndPanel.SetActive(true);
                // Display game results, winner, etc.
                break;

            default:
                Debug.LogWarning("Unhandled GamePhase: " + currentPhase);
                break;
        }
    }

    // Helper method to deactivate all UI panels
    private void DeactivateAllPanels()
    {
        mafiaPanel.SetActive(false);
        doctorPanel.SetActive(false);
        detectivePanel.SetActive(false);
        civilianPanel.SetActive(false);
        chatPanel.SetActive(false);
        votingPanel.SetActive(false);
        gameEndPanel.SetActive(false);
        lobbyPanel.SetActive(false);

        // Also clear dynamically generated lists
        ClearPlayerList(doctorPlayerListContent);
        ClearPlayerList(detectivePlayerListContent);
        ClearPlayerList(votingPlayerListContent);
    }

    // Clears all child GameObjects from a given Transform
    private void ClearPlayerList(Transform contentParent)
    {
        if (contentParent == null) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    // Dynamically populates a Scroll View's content with player buttons
    private void PopulatePlayerSelectionList(Transform contentParent, List<PlayerState> players, Action<string> onPlayerSelected)
    {
        ClearPlayerList(contentParent); // Clear existing buttons first

        foreach (var player in players)
        {
            // Instantiate the prefab as a child of the contentParent
            GameObject playerItem = Instantiate(playerListItemPrefab, contentParent);

            // Get the Button component and its Text child
            Button button = playerItem.GetComponent<Button>();
            Text buttonText = playerItem.GetComponentInChildren<Text>(); // Assumes Text is a child of the button

            if (buttonText != null)
            {
                buttonText.text = player.name; // Set the button's text to the player's name
            }

            if (button != null)
            {
                // Add a listener to the button's click event
                // Use a lambda expression to capture the player.id for the callback
                button.onClick.AddListener(() => onPlayerSelected?.Invoke(player.id));
            }
        }
    }


    // --- Example Button Click Handlers ---
    // These methods would be linked to your UI Buttons in the Inspector.
    // They would then communicate with your Game Manager or other game logic scripts.

    public void OnMafiaKillButtonClicked()
    {
        if (selectedPlayerIdForAction != null)
        {
            Debug.Log($"Mafia attempting to kill: {selectedPlayerIdForAction}");
            // Example: GameManager.Instance.PerformMafiaKill(selectedPlayerIdForAction);
            // You'd likely send an RPC to the GameManager (on the Master Client)
            // PhotonView.RPC("RPC_PerformMafiaKill", RpcTarget.MasterClient, selectedPlayerIdForAction);
            selectedPlayerIdForAction = null; // Clear selection after action
        }
        else
        {
            Debug.Log("Please select a player to kill.");
        }
    }

    public void OnDoctorSaveButtonClicked()
    {
        if (selectedPlayerIdForAction != null)
        {
            Debug.Log($"Doctor attempting to save: {selectedPlayerIdForAction}");
            // Example: GameManager.Instance.PerformDoctorSave(selectedPlayerIdForAction);
            // PhotonView.RPC("RPC_PerformDoctorSave", RpcTarget.MasterClient, selectedPlayerIdForAction);
            selectedPlayerIdForAction = null;
        }
        else
        {
            Debug.Log("Please select a player to save.");
        }
    }

    public void OnDetectiveInvestigateButtonClicked()
    {
        if (selectedPlayerIdForAction != null)
        {
            Debug.Log($"Detective attempting to investigate: {selectedPlayerIdForAction}");
            // Example: GameManager.Instance.PerformDetectiveInvestigate(selectedPlayerIdForAction);
            // PhotonView.RPC("RPC_PerformDetectiveInvestigate", RpcTarget.MasterClient, selectedPlayerIdForAction);
            selectedPlayerIdForAction = null;
        }
        else
        {
            Debug.Log("Please select a player to investigate.");
        }
    }

    public void OnVoteConfirmButtonClicked()
    {
        if (selectedPlayerIdForAction != null)
        {
            Debug.Log($"Confirming vote for: {selectedPlayerIdForAction}");
            // Example: GameManager.Instance.RecordVote(PhotonNetwork.LocalPlayer.UserId, selectedPlayerIdForAction);
            // PhotonView.RPC("RPC_RecordVote", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.UserId, selectedPlayerIdForAction);
            selectedPlayerIdForAction = null;
        }
        else
        {
            Debug.Log("Please select a player to vote for.");
        }
    }

    // --- Photon Callbacks (Example for Integration) ---
    // In a real Photon game, you'd use these to trigger UI updates.

    // This is called when the local player successfully joins a room.
    public override void OnJoinedRoom()
    {
        if (photonView.IsMine)
        {
            // Simulate initial state for testing. In a real game, GameManager would call UpdateUI.
            // You'd replace this with actual game state management.
            // Example:
            // List<PlayerState> mockPlayers = new List<PlayerState>
            // {
            //     new PlayerState { id = "player1", name = "Alice", role = "Civilian", isAlive = true },
            //     new PlayerState { id = "player2", name = "Bob", role = "Mafia", isAlive = true },
            //     new PlayerState { id = "player3", name = "Charlie", role = "Doctor", isAlive = true }
            // };
            // UpdateUI(GamePhase.Lobby, "Civilian", true, mockPlayers);
        }
    }

    // Example of how a game manager might call UpdateUI via RPC or custom properties
    // This method would be called by the Game Manager when the phase changes
    [PunRPC]
    public void RPC_UpdateGamePhaseAndRole(int phaseInt, string roleString, string[] alivePlayerIds, string[] alivePlayerNames, string[] alivePlayerRoles)
    {
        GamePhase phase = (GamePhase)phaseInt;
        string role = roleString;

        // Reconstruct alivePlayers list from RPC parameters
        List<PlayerState> alivePlayers = new List<PlayerState>();
        for (int i = 0; i < alivePlayerIds.Length; i++)
        {
            alivePlayers.Add(new PlayerState
            {
                id = alivePlayerIds[i],
                name = alivePlayerNames[i],
                role = alivePlayerRoles[i],
                isAlive = true // Assuming only alive players are sent
            });
        }

        // Ensure this UI update only happens for the relevant client
        UpdateUI(phase, role, photonView.IsMine, alivePlayers);
    }

    // You would typically have a Game Manager script that calls this UIManager's UpdateUI method
    // when the game state (phase, roles) changes, often synchronized via Photon.
}
