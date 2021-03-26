using Photon.Pun;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class GameplayController : MonoBehaviour {

    //gController Settings
    #region
    [Header("gController Settings:")]
    [Tooltip("The Prefab used for the Main Menu.")]
    [SerializeField]
    private GameObject MainMenuPrefab; // Saved via Inspector.
    [Tooltip("Time (in seconds) for Prep-Phase Count-Down.")]
    [SerializeField]
    private int CountDownTimeInSeconds;
    [Tooltip("Time (in seconds) for Active-Game Count-Down.")]
    [SerializeField]
    private int GameTimeDurationInSeconds;
    [Tooltip("A list of maps that will randomly be selected from.")]
    [SerializeField]
    private List<GameObject> mapList = new List<GameObject>(); // List of possible maps.
    #endregion


    //Vars
    #region
    [SerializeField]
    private List<GameObject> propsSpawnedDuringGame = new List<GameObject>();
    private List<int> InGamePlayerList = new List<int>();
    private List<int> SeekerPlayerList = new List<int>();
    private List<int> PropPlayerList = new List<int>();
    private int gameplayState = -1; // -1 is initial load, 0 is Main Menu, 1 is Pre-Game Room, 2 is In-Game Prep-Phase, 3 is In-Game Active, 4 is In-Game End-Of-Round;
    private PhotonView gcpv;
    private ScreenController sController;
    private AudioController aController;
    private PlayerPropertiesController ppc;
    private MapProperties mp;
    private GameObject currentMapLoaded; //This is the current map prefab loaded. Could be pre-game lobby, office map, candy land map, etc.
    private int CurrentCountDownTimer = 20;
    private int CurrentGameTimeLeftTimer = 300;
    [SerializeField]
    private int CurrentTeam = -1; // -1 default, 0 is props, 1 is seeker.
    #endregion


    private void Awake() {
        gcpv = GetComponent<PhotonView>();
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        aController = GameObject.FindGameObjectWithTag("AudioController").GetComponent<AudioController>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
    }

    private void Update() {

        // Just a temporary lock/unlock cursor override button. P.
        if (Input.GetKeyDown(KeyCode.P)) {
            if (gameplayState == 0) { // If we're in the main menu.
                if (Cursor.lockState == CursorLockMode.Locked) {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                } else {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }



    //Updates gameplayState and Logs it. Anyone can run this func, but it only works on the MasterClient.
    public void UpdateGameplayState(int newState) {
        gameplayState = newState;
        if (PhotonNetwork.InRoom) {
            if (PhotonNetwork.IsMasterClient) {
                if (newState == 0) { // Main Menu.
                    EnableMainMenuPrefab(true);
                    CancelInvoke("Invoke_CountdownPrepPhase");
                } else if (newState == 1) { // Into Pre-Game Room.
                    MoveAllToPreGameLobby();
                } else if (newState == 2) { // In-Game Prep Phase.
                    MoveAllToFreshGame();
                } else if (newState == 3) { // In-Game Active Phase.

                } else if (newState == 4) { // In-Game End Phase.
                    RunEndPhase();
                }
            }
        } else { // If we're not in a room yet.
            if (newState == 0) { // Main Menu.
                EnableMainMenuPrefab(true);
            }
        }
        Debug.Log("Gameplay State Updated! " + newState.ToString());
    }

    //This is run from PPC when the MC detects a player left unexpectedly. Gotta remove leavingPlayers from the playerLists.
    public void MasterClientRemovesPlayerFromListOnDisconnect(int plyID) {
        Debug.Log("Trying to remove this ID from all lists: " + plyID.ToString());
        if (PhotonNetwork.IsMasterClient) {
            if (PhotonNetwork.InRoom) {
                if (gameplayState > 1 && gameplayState < 4) { // If we're in a game AND it's not the "end phase".
                    if (SeekerPlayerList.Contains(plyID)) {
                        SeekerPlayerList.Remove(plyID);
                    } else if (PropPlayerList.Contains(plyID)) {
                        PropPlayerList.Remove(plyID);
                    } else {
                        Debug.LogError("Tried to remove player's plyID from seeker/prop player lists and couldn't find it?..");
                    }

                    if (InGamePlayerList.Contains(plyID)) {
                        InGamePlayerList.Remove(plyID);
                    } else {
                        Debug.LogError("Tried to remove disonnecting player from InGamePlayerList and it was never in there in the first place?..");
                    }
                }
            }
        }
    }

    //Main Menu "scene" Toggle.
    public void EnableMainMenuPrefab(bool result) {
        if (result) {
            if (MainMenuPrefab.activeSelf == false) {
                MainMenuPrefab.SetActive(true);
            }
        } else {
            if (MainMenuPrefab.activeSelf == true) {
                MainMenuPrefab.SetActive(false);
            }
        }
    }

    //Ran locally by a single seeker from PlayerMovement.
    public void RequestToDestroyVacantProp(int vacantPropID) {
        gcpv.RPC("RPC_RequestToDestroyVacantProp", RpcTarget.MasterClient, PhotonView.Find(vacantPropID).ViewID); //We'll be vacuuming props into cannisters. So we can afford a RPC round-trip.
    }

    //Ran locally by a single seeker from PlayerMovement.
    public void RequestToKillPropPlayer(int killedPlyID) {
        gcpv.RPC("RPC_RequestToKillPropPlayer", RpcTarget.MasterClient, PhotonView.Find(killedPlyID).ViewID); //We'll be vacuuming props into cannisters. So we can afford a RPC round-trip.
    }
    
    //Ran locally from any client when they takeover a prop in PlayerMovement.
    public void TellMasterClientToAddPropToDestroyList(GameObject objToDestroy) {
        // Make sure the GameObject we send is a PROP, or likewise.
        if (objToDestroy.GetComponent<PropInteraction>()) {

            // Get a reference to the target PV.
            PhotonView destroyObjPV = objToDestroy.GetPhotonView();

            // Is target PV null?
            if (destroyObjPV != null) {
                gcpv.RPC("RPC_AddPropToDestroyOnRoundOver", RpcTarget.MasterClient, destroyObjPV.ViewID);
            }

        }
    }


    private void DestroyObjectsOnMapSwitch() {
        if (PhotonNetwork.IsMasterClient) {
            foreach (GameObject objToDestroy in propsSpawnedDuringGame) {
                //If our prop isn't null.
                if (objToDestroy != null) {
                    //If our prop doesn't have the AttachedProp tag or doesn't have a parent of any kind, we should be able to safely Net-Destroy it.
                    if (objToDestroy.tag != "AttachedProp" || objToDestroy.transform.parent == null) {

                        objToDestroy.tag = "KOS";

                        // Make sure it gets owned by the master client before removal.
                        objToDestroy.GetPhotonView().TransferOwnership(PhotonNetwork.MasterClient);

                        //OwnershipTransfered Callback on PropInteraction will handle the rest.
                    }
                }
            }

            // After we destroyed all the props we needed to, let's clear the list so it can be rebuilt at a later time.
            propsSpawnedDuringGame.Clear();

        }
    }

    //While the game is active, we'll check to see if there's any reason to close the game. (Not enough seekers/props.)
    private void CheckIfGameShouldAutoClose() {
        if (PhotonNetwork.IsMasterClient) {
            if (PhotonNetwork.InRoom) {
                if (gameplayState > 1 && gameplayState < 4) { // If we're in a game AND it's not the "end phase".
                    if (PhotonNetwork.CurrentRoom.PlayerCount == InGamePlayerList.Count) {
                        if (InGamePlayerList.Count < 2) {
                            UpdateGameplayState(4); // End-Phase. 
                            CancelInvoke("Invoke_UpdateGameTimeLeft");
                        } else if (SeekerPlayerList.Count == 0) {
                            UpdateGameplayState(4); // End-Phase. Props Win
                            CancelInvoke("Invoke_UpdateGameTimeLeft");
                        } else if (PropPlayerList.Count == 0) {
                            UpdateGameplayState(4); // End-Phase. Seeker Win
                            CancelInvoke("Invoke_UpdateGameTimeLeft");
                        }
                        // NOTE:
                        // When player's disconnect tell the host to remove those players from the player lists accordingly.
                    }
                }
            }
        }
    }

    //Only runs on MasterClient. This runs under UpdateGameplayState(4).
    private void RunEndPhase() {
        int loadingScreenRoutine = 3;
        gcpv.RPC("RPC_RunEndPhase", RpcTarget.AllBuffered, loadingScreenRoutine);
    }

    //Only runs on MasterClient. Ran in UpdateGameplayState(1)
    private void MoveAllToPreGameLobby() {
        //Destroy all props left from last game if there are any.
        DestroyObjectsOnMapSwitch();

        // We disable MainMenuProp in RoomSystem when the OnJoined Callback is ran. This happens on first join.
        if (currentMapLoaded != null) {
            PhotonNetwork.Destroy(currentMapLoaded);
            currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PreGameLobby"), Vector3.zero, Quaternion.identity, 0);
            gcpv.RPC("RPC_MoveAllToPreGameLobby", RpcTarget.AllBuffered);
        } else {
            currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PreGameLobby"), Vector3.zero, Quaternion.identity, 0);
        }
    }

    //Only runs on MasterClient. Ran in UpdateGameplayState(3)
    private void MoveAllToFreshGame() {
        PropPlayerList.Clear();
        InGamePlayerList.Clear();
        SeekerPlayerList.Clear();
        int loadingScreenRoutine = 2;
        gcpv.RPC("RPC_MoveAllToFreshGame", RpcTarget.AllBuffered, loadingScreenRoutine);
    }

    //RPC's **********************************************************************************************************************************************************************************
    #region


    //Runs on all clients.
    [PunRPC]
    private void RPC_MoveAllToFreshGame(int loadingScreenRoutine) {
        sController.RunLoadingScreen(loadingScreenRoutine); // Start a loading screen.
        sController.ResetHauntValue(); // Resets client-side value for the hauntBar.
        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer"); // Reference our localplayer.
        int myPV = localPlayer.GetPhotonView().ViewID; // Reference our localPlayer's ViewID to send it to MasterClient for PlayerList.
        gcpv.RPC("RPC_HelpMasterBuildPlayerList", RpcTarget.MasterClient, myPV);
        ppc.moveState = 1; // pre-prop moveState.
    }

    // Runs on all clients.
    [PunRPC]
    private void RPC_MoveAllToPreGameLobby() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer"); // Reference our localplayer.
        GameObject rController = GameObject.FindGameObjectWithTag("RoomController");
        Rigidbody plyRB = localPlayer.GetComponent<Rigidbody>();
        if (localPlayer.gameObject.transform.Find("PropHolder").childCount > 0) {
            PhotonNetwork.Destroy(localPlayer.gameObject.transform.Find("PropHolder").GetChild(0).gameObject); // Destroy our child object.
        }
        plyRB.isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
        localPlayer.transform.position = rController.transform.GetChild(Random.Range(0, rController.transform.childCount - 1)).position;
        localPlayer.transform.rotation = Quaternion.identity;
        //Passing 0 = PropSpawner. Passing 1 = Player-takover spawn. Passing 2 = Player Becoming Ghost/Seeker. Passing 3 = Dead Prop Becoming Trans Ghost.
        object[] newRoomFromOldRoomInstData = new object[1];
        newRoomFromOldRoomInstData[0] = 2;
        ppc.moveState = 1; // pre-prop moveState.
        CurrentTeam = -1; // -1 default, 0 prop, 1 seeker. TeamNumber only goes above -1 if we're in an active game.
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost"), localPlayer.gameObject.transform.position, localPlayer.gameObject.transform.rotation, 0, newRoomFromOldRoomInstData);
    }

    // Runs on all clients.
    [PunRPC]
    private void RPC_RunEndPhase(int loadingScreenRoutine) {
        PhotonView lpPV = GameObject.FindGameObjectWithTag("LocalPlayer").GetComponent<PhotonView>();
        sController.RunLoadingScreen(loadingScreenRoutine); // Start a loading screen.
        sController.DisplayHauntBar(false);
        CancelInvoke("Invoke_UpdateGameTimeLeft");
        CancelInvoke("Invoke_CountdownPrepPhase");
        sController.UpdateGameTimeLeft(0); // Try to clear timer text.
        Invoke("Invoke_EndPhaseBuffer", 1f);
    }

    //Only runs on MasterClient.
    [PunRPC]
    void RPC_RequestToDestroyVacantProp(int vacantPropID) {
        PhotonView vacantProp = PhotonView.Find(vacantPropID);
        if (vacantProp != null) {
            Debug.Log("Seeker incorrectly chose to destroy: " + vacantProp.name + ", vacant prop destroyed over the network.");
            // Make sure our MC owns it. (Causes empty network calls to object. MC should just be able to destroy.)
            //objToDestroy.GetPhotonView().RequestOwnership();
            // Destroy it.
            PhotonNetwork.Destroy(vacantProp.gameObject);
        } else {
            Debug.LogError("Request to kill prop player denied. It's null now?");
        }
    }

    //Only runs on MasterClient.
    [PunRPC]
    void RPC_RequestToKillPropPlayer(int killedPlyID) {
        PhotonView deadID = PhotonView.Find(killedPlyID);
        if (deadID != null) {
            if (PropPlayerList.Contains(deadID.ViewID)) {
                PropPlayerList.Remove(deadID.ViewID);
            }
            Debug.Log("Prop player: " + deadID.name + " has been exterminated. Remaining props: " + PropPlayerList.Count);
            gcpv.RPC("RPC_ApproveKillPropPlayer", RpcTarget.AllBuffered, killedPlyID);
        } else {
            Debug.LogError("Request to kill prop player denied. It's null now?");
        }
    }

    //Runs on every client.
    [PunRPC]
    void RPC_ApproveKillPropPlayer(int killedPlyID) {
        PhotonView deadID = PhotonView.Find(killedPlyID);
        if (deadID != null) {
            Destroy(deadID.gameObject.transform.Find("PropHolder").GetChild(0).gameObject); // Everyone will destroy the child object.
            deadID.GetComponent<Rigidbody>().isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
            deadID.gameObject.transform.rotation = Quaternion.identity;

            if (deadID.IsMine) { // If we own the player locally.
                //Passing 0 = PropSpawner. Passing 1 = Player-takover spawn. Passing 2 = Player Becoming Ghost/Seeker. Passing 3 = Dead Prop Becoming Trans Ghost.
                object[] deathInstanceData = new object[1];
                deathInstanceData[0] = 3;
                ppc.moveState = 4; //We're a seeker now.
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost_Trans"), deadID.gameObject.transform.position, deadID.gameObject.transform.rotation, 0, deathInstanceData);
            }

        } else {
            Debug.LogError("Trying to destroy object but it's null?");
        }
    }

    //Each player in room tells master client to run this. All players send info to master so he can make a list.
    [PunRPC]
    private void RPC_HelpMasterBuildPlayerList(int plyID) {
        int loopCount = 0;
        InGamePlayerList.Add(plyID); // Add each player to a list.
        if (InGamePlayerList.Count == PhotonNetwork.CurrentRoom.PlayerCount) { // Does our newly completed list match the PhotonNetwork playerlist?
            foreach (int plyIDToSort in InGamePlayerList) {
                loopCount++; //This is used to count the # of times we loop.
                if (InGamePlayerList.Count < 6) { //If there are 5 players, use one seeker.
                    if (loopCount != InGamePlayerList.Count) {// If this is NOT the last loop.
                        int determineRole = Random.Range(0, 4);
                        if (determineRole == 0) { // Rolled Seeker
                            if (SeekerPlayerList.Count < 1) {
                                SeekerPlayerList.Add(plyIDToSort);
                            } else {// There's already a seeker. We need to place you as pre-prop.
                                PropPlayerList.Add(plyIDToSort);
                            }
                        } else if (determineRole > 0) {// Rolled Pre Prop
                            PropPlayerList.Add(plyIDToSort);
                        }
                    } else { // This is the last loop. Make sure we add a seeker if there isn't one yet.
                        if (SeekerPlayerList.Count < 1) {
                            SeekerPlayerList.Add(plyIDToSort);
                        } else {
                            PropPlayerList.Add(plyIDToSort);
                        }
                    }
                } else { // If there are more than 5 players, use two seekers.
                    if (loopCount < InGamePlayerList.Count - 1) {// If this is NOT the last two loops.
                        int determineRole = Random.Range(0, 4);
                        if (determineRole == 0) { // Rolled Seeker
                            if (SeekerPlayerList.Count < 2) {
                                SeekerPlayerList.Add(plyIDToSort);
                            } else {// There's already a seeker. We need to place you as pre-prop.
                                PropPlayerList.Add(plyIDToSort);
                            }
                        } else if (determineRole > 0) {// Rolled Pre Prop
                            PropPlayerList.Add(plyIDToSort);
                        }
                    } else { // This is the last two loops. Make sure we add two seekers if there isn't one yet.
                        if (SeekerPlayerList.Count < 2) {
                            SeekerPlayerList.Add(plyIDToSort);
                        } else {
                            PropPlayerList.Add(plyIDToSort);
                        }
                    }
                }
                if (SeekerPlayerList.Count + PropPlayerList.Count == InGamePlayerList.Count) {
                    Debug.Log("All players have been accounted for and sorted.");
                    loopCount = 0; // Reset loopCount for next time.
                    //Send our lists to all players. Must send them as array, and unpack them into list when we receive them.
                    gcpv.RPC("RPC_SpawnSortedPlayersIntoFreshGame", RpcTarget.AllBuffered, InGamePlayerList.ToArray(), SeekerPlayerList.ToArray(), PropPlayerList.ToArray());
                }
            }
        }
    }

    // This runs on all players in the room. Sent from MasterClient.
    [PunRPC] // All we're doing here is spawning the map if we're host, then all players will set their moveState accoring to what the master told them.
    private void RPC_SpawnSortedPlayersIntoFreshGame(int[] playerList, int[] seekerList, int[] propList) {

        // We'll send the already packed ID lists to the map's instantiationData to move players to correct locations after they spawn.
        object[] listData = new object[3];
        listData[0] = seekerList;
        listData[1] = propList;

        // Unpack our int[] arrays to List<int>. 
        List<int> allPlayerList = playerList.ToList<int>();
        List<int> allSeekerList = seekerList.ToList<int>();
        List<int> allPropList = propList.ToList<int>();

        int myID = -1;

        // Destroy all props being controller by players.
        foreach (int plyID in allPlayerList) {
            PhotonView plyIDPV = PhotonView.Find(plyID);
            // Destroy all children of PropHolder.
            foreach (Transform child in plyIDPV.gameObject.transform.Find("PropHolder").transform) {
                Destroy(child.gameObject);
            }
            if (plyIDPV.IsMine) { // When we found our player's prop, let's save the viewID.
                myID = plyIDPV.ViewID;
            }
        }


        //Our master client spawns in the map.
        if (PhotonNetwork.IsMasterClient) { // Only if we're host to we spawn the map and destroy the old one over the network.
            if (currentMapLoaded != null) {
                //Destroy the map prefab.
                PhotonNetwork.Destroy(currentMapLoaded);
                //Destroy all the props from the map.
                DestroyObjectsOnMapSwitch();
                //Spawn the new map.
                int r = Random.Range(0, mapList.Count - 1);
                currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", mapList[r].name), Vector3.zero, Quaternion.identity, 0, listData);
            }
        }

        //Passing 0 = PropSpawner. Passing 1 = Player-takover spawn. Passing 2 = Player Becoming Ghost/Seeker.
        //We must send instantiation data with object when we spawn it. We do this to determine if it was spawned by a player, or by a prop-spawner, or by a freshGame reset.
        object[] instanceData = new object[1];
        instanceData[0] = 2;



        foreach (int seekerID in allSeekerList) {
            if (myID == seekerID) {
                //We're a seeker.
                CurrentTeam = 1; // -1 default, 0 prop, 1 seeker.
                PhotonView ourPV = PhotonView.Find(myID);
                ourPV.GetComponent<Rigidbody>().isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
                ppc.moveState = 3; //We're a seeker now.
                ourPV.gameObject.transform.rotation = Quaternion.identity;
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Seeker"), ourPV.gameObject.transform.position, ourPV.gameObject.transform.rotation, 0, instanceData); //Spawn our ghost prop.
            }
        }

        foreach (int propID in allPropList) {
            if (myID == propID) {
                //We're a pre-prop ghost.
                CurrentTeam = 0; // -1 default, 0 prop, 1 seeker.
                PhotonView ourPV = PhotonView.Find(myID);
                ourPV.GetComponent<Rigidbody>().isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
                ourPV.gameObject.transform.rotation = Quaternion.identity;
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost"), ourPV.gameObject.transform.position, ourPV.gameObject.transform.rotation, 0, instanceData); //Spawn our ghost prop.
            }
        }


        Invoke("Invoke_MoveAllToFreshGame", 0.5f);
    }

    //Runs on all clients.
    [PunRPC]
    private void RPC_OpenSeekerGate() {
        mp = GameObject.FindGameObjectWithTag("Map").GetComponent<MapProperties>();

        if (mp.seekerDoor != null) {
            Destroy(mp.seekerDoor);
        } else {
            Debug.LogError("Tried to destroy Seeker door. It was null?..");
        }


        List<GameObject> oppositeTeamNameTagList = GameObject.FindGameObjectsWithTag("NameTag").ToList<GameObject>();

        //After all lists are searched for my id, let's disable nametags of props if we're a seeker.

        if (CurrentTeam == 1) { // If we're on the Seeker team
            foreach (int propPlayerID in PropPlayerList) {
                foreach (GameObject nt in oppositeTeamNameTagList) {
                    if (nt.GetComponent<NameTagHolder>().ownerID == propPlayerID) {
                        nt.GetComponent<CanvasGroup>().alpha = 0;
                    }
                }
            }
        }

    }

    //Ran on MasterClient. Adds object to list for destruction.
    [PunRPC]
    private void RPC_AddPropToDestroyOnRoundOver(int objToDestroyID) {
        PhotonView objToDestroyPV = PhotonView.Find(objToDestroyID);
        if (objToDestroyPV != null) {
            propsSpawnedDuringGame.Add(objToDestroyPV.gameObject);
        } else {
            Debug.LogWarning("MasterClient tried to add object: " + objToDestroyPV.gameObject.name + " to the list of objects to destroy on map change, but object is null.");
        }
    }


    #endregion

    //Invokes **********************************************************************************************************************************************************************************
    #region

    // This invoke moves all players into a fresh prep-phase game.
    private void Invoke_MoveAllToFreshGame() {
        CurrentGameTimeLeftTimer = GameTimeDurationInSeconds; // We set our gameTime timer equal to the "start" time.
        CurrentCountDownTimer = CountDownTimeInSeconds; // We set our countdown timer equal to the "start" time.
        InvokeRepeating("Invoke_CountdownPrepPhase", 0.1f, 1f);

        //End the loading screen once we're done.
        sController.EndLoadingScreen(2f);
    }

    // This is called through Invoke.Repeating. Effectively just a counter for prep-phase.
    private void Invoke_CountdownPrepPhase() {
        CurrentCountDownTimer--;
        if (CurrentCountDownTimer > 0) { //Counting down.
            sController.UpdateCountDown(CurrentCountDownTimer);
            aController.PlayCountDownTick();
        } else if (CurrentCountDownTimer == 0) { // Last countdown tick.
            sController.UpdateCountDown(CurrentCountDownTimer);
            aController.PlayCountDownLastTick();
            if (PhotonNetwork.IsMasterClient) {
                UpdateGameplayState(3); // Move game to active phase.
                gcpv.RPC("RPC_OpenSeekerGate", RpcTarget.AllBuffered);
            }
            CancelInvoke("Invoke_CountdownPrepPhase");
            InvokeRepeating("Invoke_UpdateGameTimeLeft", 0.1f, 1f);
        }
    }

    // This is called through Invoke.Repeating. Effectively just a counter for Active-Game phase.
    private void Invoke_UpdateGameTimeLeft() {
        CurrentGameTimeLeftTimer--;
        if (CurrentGameTimeLeftTimer > 0) { //Counting down.
            sController.UpdateGameTimeLeft(CurrentGameTimeLeftTimer);
            CheckIfGameShouldAutoClose(); // We're checking to see if we should close the game due to lack of players or other coniditions.
        } else if (CurrentGameTimeLeftTimer == 0) { // Last countdown tick.
            sController.UpdateGameTimeLeft(CurrentGameTimeLeftTimer);
            if (PhotonNetwork.IsMasterClient) {
                UpdateGameplayState(4); // Move game to end-phase
            }
            CancelInvoke("Invoke_UpdateGameTimeLeft");
        }
    }

    // After we've showed the loadingScreen on the way out of the game, here we end that loading screen and UpdateGameplayState to 1. Back to the room.
    private void Invoke_EndPhaseBuffer() {
        if (PhotonNetwork.IsMasterClient) {
            UpdateGameplayState(1);
        }
        sController.EndLoadingScreen(2f);
    }

    #endregion

}
