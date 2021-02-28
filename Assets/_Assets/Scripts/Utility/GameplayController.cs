using Photon.Pun;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class GameplayController : MonoBehaviour {

    [SerializeField]
    private GameObject MainMenuPrefab; // Saved via inspector.
    [SerializeField]
    private int gameplayState = -1; // -1 is initial load, 0 is Main Menu, 1 is Pre-Game Room, 2 is In-Game Prep-Phase, 3 is In-Game Active, 4 is In-Game End-Of-Round;
    [SerializeField]
    private List<GameObject> mapList = new List<GameObject>(); // List of possible maps.
    [SerializeField]
    private List<int> InGamePlayerList = new List<int>();
    [SerializeField]
    private List<int> SeekerPlayerList = new List<int>();
    [SerializeField]
    private List<int> PropPlayerList = new List<int>();


    //Settings:
    [SerializeField]
    private int CountDownTimeInSeconds;

    private PhotonView gcpv;
    private ScreenController sController;
    private AudioController aController;
    private PlayerPropertiesController ppc;
    private MapProperties mp;
    private GameObject currentMapLoaded; //This is the current map prefab loaded. Could be pre-game lobby, office map, candy land map, etc.
    private int CurrentCountDownTimer = 20;



    private void Awake() {
        gcpv = GetComponent<PhotonView>();
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        aController = GameObject.FindGameObjectWithTag("AudioController").GetComponent<AudioController>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
    }


    //Updates gameplayState and Logs it. //Anyone can run this func, but it only works on the MasterClient.
    public void UpdateGameplayState(int newState) {
        gameplayState = newState;
        if (PhotonNetwork.IsConnectedAndReady) {
            if (PhotonNetwork.InRoom) {
                if (PhotonNetwork.IsMasterClient) {
                    if (newState == 0) { // Main Menu.
                        EnableMainMenuPrefab(true);
                    } else if (newState == 1) { // Into Pre-Game Room.
                        MoveAllToPreGameLobby();
                    } else if (newState == 2) { // In-Game Prep Phase.
                        MoveAllToFreshGame();
                    } else if (newState == 3) { // In-Game Active Phase.

                    } else if (newState == 4) { // In-Game End Phase.

                    }
                }
            } else { // If we're not in a room yet.
                if (newState == 0) { // Main Menu.
                    EnableMainMenuPrefab(true);
                }
            }
            Debug.Log("Gameplay State Updated! " + newState.ToString());
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
    public void RequestToKillPropPlayer(int killedPlyID) {
        gcpv.RPC("RPC_RequestToKillPropPlayer", RpcTarget.MasterClient, PhotonView.Find(killedPlyID)); //We'll be vacuuming props into cannisters. So we can afford a RPC round-trip.
    }

    //Only runs on MasterClient.
    private void MoveAllToPreGameLobby() {
        // We disable MainMenuProp in RoomSystem when the OnJoined Callback is ran.
        currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PreGameLobby"), Vector3.zero, Quaternion.identity, 0);
    }

    //Only runs on MasterClient.
    private void MoveAllToFreshGame() {
        int loadingScreenRoutine = 2;
        gcpv.RPC("RPC_MoveAllToFreshGame", RpcTarget.AllBuffered, loadingScreenRoutine);
    }

    //RPC's *********
    //Runs on all clients.
    [PunRPC]
    private void RPC_MoveAllToFreshGame(int loadingScreenRoutine) {
        sController.RunLoadingScreen(loadingScreenRoutine); // Start a loading screen.
        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer"); // Reference our localplayer.
        int myPV = localPlayer.GetPhotonView().ViewID; // Reference our localPlayer's ViewID to send it to MasterClient for PlayerList.
        gcpv.RPC("RPC_HelpMasterBuildPlayerList", RpcTarget.MasterClient, myPV);
        ppc.moveState = 1; // pre-prop moveState.
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
            if (PropPlayerList.Count == 0) {
                Debug.LogWarning("GAME OVER! ALL PROPS KILLED!");
            }
            gcpv.RPC("RPC_ApproveKillPropPlayer", RpcTarget.MasterClient, killedPlyID);
        } else {
            Debug.LogError("Request to kill prop player denied. It's null now?");
        }
    }

    //Runs on every client.
    [PunRPC]
    void RPC_ApproveKillPropPlayer(int killedPlyID) {
        PhotonView deadID = PhotonView.Find(killedPlyID);
        if (deadID != null) {
            Destroy(deadID.gameObject.transform.Find("PropHolder").GetChild(0)); // Everyone will destroy the child object.

            if (deadID.IsMine) { // If we own the player locally.
                //Passing 0 = PropSpawner. Passing 1 = Player-takover spawn. Passing 2 = Player Becoming Ghost/Seeker. Passing 3 = Dead Prop Becoming Trans Ghost.
                object[] deathInstanceData = new object[1];
                deathInstanceData[0] = 3;

                deadID.GetComponent<Rigidbody>().isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
                ppc.moveState = 4; //We're a seeker now.
                deadID.gameObject.transform.rotation = Quaternion.identity;
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost_Trans"), deadID.gameObject.transform.position, deadID.gameObject.transform.rotation, 0, deathInstanceData);
            }

        }
    }

    //Each player in room tells master client to run this.
    [PunRPC]
    private void RPC_HelpMasterBuildPlayerList(int plyID) {
        InGamePlayerList.Add(plyID); // Add each player to a list.
        if (InGamePlayerList.Count == PhotonNetwork.CurrentRoom.PlayerCount) { // Does our newly completed list match the PhotonNetwork playerlist?
            foreach (int plyIDToSort in InGamePlayerList) {
                if (InGamePlayerList.Count < 6) { //If there are 5 players, use one seeker.
                    if (SeekerPlayerList.Count < 1) {
                        SeekerPlayerList.Add(plyIDToSort); // Add our seekers to the seeker list.
                    } else {
                        PropPlayerList.Add(plyIDToSort); // Add our props to the prop list.
                    }
                } else { // If there are more than 5 players, use two seekers.
                    if (SeekerPlayerList.Count < 2) {
                        SeekerPlayerList.Add(plyIDToSort);
                    } else {
                        PropPlayerList.Add(plyIDToSort);
                    }
                }
                if (SeekerPlayerList.Count + PropPlayerList.Count == InGamePlayerList.Count) {
                    Debug.Log("All players have been accounted for and sorted.");
                    //Send our lists to all players. Must send them as array, and unpack them into list when we receive them.
                    gcpv.RPC("RPC_SpawnSortedPlayersIntoFreshGame", RpcTarget.AllBuffered, InGamePlayerList.ToArray(), SeekerPlayerList.ToArray(), PropPlayerList.ToArray());
                }
            }
        }
    }

    [PunRPC] // This runs on all players in the room. Sent from MasterClient.
    private void RPC_SpawnSortedPlayersIntoFreshGame(int[] playerList, int[] seekerList, int[] propList) {

        //We'll send the already packed ID lists to the map's instantiationData to move players to correct locations after they spawn.
        object[] listData = new object[3];
        listData[0] = seekerList;
        listData[1] = propList;

        //Unpack our int[] arrays to List<int>. 
        List<int> allPlayerList = playerList.ToList<int>();
        List<int> allSeekerList = seekerList.ToList<int>();
        List<int> allPropList = propList.ToList<int>();

        int myID = -1;

        //Destroy all props being controller by players.
        foreach (int plyID in allPlayerList) {
            PhotonView plyIDPV = PhotonView.Find(plyID);
            Destroy(plyIDPV.gameObject.transform.Find("PropHolder").transform.GetChild(0).gameObject);
            if (plyIDPV.IsMine) { // When we found our player's prop, let's save the viewID.
                myID = plyIDPV.ViewID;
            }
        }


        //Our master client spawns in the map.
        if (PhotonNetwork.IsMasterClient) { // Only if we're host to we spawn the map and destroy the old one over the network.
            if (currentMapLoaded != null) {
                PhotonNetwork.Destroy(currentMapLoaded);
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
                PhotonView ourPV = PhotonView.Find(myID);
                ourPV.GetComponent<Rigidbody>().isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
                ourPV.gameObject.transform.rotation = Quaternion.identity;
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost"), ourPV.gameObject.transform.position, ourPV.gameObject.transform.rotation, 0, instanceData); //Spawn our ghost prop.
            }
        }

        List<GameObject> oppositeTeamNameTagList = GameObject.FindGameObjectsWithTag("NameTag").ToList<GameObject>();

        //After all lists are searched for my id, let's disable nametags of props if we're a seeker.
        if (allSeekerList.Contains(myID)) { // If we're on the Seeker team
            foreach (int propPlayerID in PropPlayerList) { //
                foreach(GameObject nt in oppositeTeamNameTagList) {
                    if (nt.GetComponent<NameTagHolder>().ownerID == propPlayerID) {
                        nt.GetComponent<CanvasGroup>().alpha = 0;
                    }
                }
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
    }


    //Invokes *********

    // This invoke moves all players into a fresh prep-phase game.
    private void Invoke_MoveAllToFreshGame() {

        CurrentCountDownTimer = CountDownTimeInSeconds; // We set our countdown timer equal to the "start" time.
        InvokeRepeating("Invoke_CountdownPrepPhase", 0.1f, 1f);

        //End the loading screen once we're done.
        sController.EndLoadingScreen(2f);
    }

    private void Invoke_CountdownPrepPhase() {
        CurrentCountDownTimer--;
        if (CurrentCountDownTimer > 0) { //Counting down.
            sController.UpdateCountDown(CurrentCountDownTimer);
            aController.PlayCountDownTick();
        } else if (CurrentCountDownTimer == 0) { // Last countdown tick. "GO!"
            sController.UpdateCountDown(CurrentCountDownTimer);
            aController.PlayCountDownLastTick();
            if (PhotonNetwork.IsMasterClient) {
                UpdateGameplayState(3);
                gcpv.RPC("RPC_OpenSeekerGate", RpcTarget.AllBuffered);
            }
            CancelInvoke("Invoke_CountdownPrepPhase");
        }
    }

}
