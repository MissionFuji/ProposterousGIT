using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class GameplayController : MonoBehaviour
{

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

    private PhotonView gcpv;
    private ScreenController sController;
    private PlayerPropertiesController ppc;
    private GameObject currentMapLoaded; //This is the current map prefab loaded. Could be pre-game lobby, office map, candy land map, etc.



    private void Awake() {
        gcpv = GetComponent<PhotonView>();
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
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

        //Our master client spawns in the map.
        if (PhotonNetwork.IsMasterClient) { // Only if we're host to we spawn the map and destroy the old one over the network.
            if (currentMapLoaded != null) {
                PhotonNetwork.Destroy(currentMapLoaded);
                int r = Random.Range(0, mapList.Count - 1);
                currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", mapList[r].name), Vector3.zero, Quaternion.identity, 0);
            }
        }


        //Unpack our int[] arrays to List<int>. 
        List<int> allPlayerList = playerList.ToList<int>();
        List<int> allSeekerList = seekerList.ToList<int>();
        List<int> allPropList = propList.ToList<int>();

        int myID = -1;

        //Destroy all props being controller by players.
        foreach(int plyID in allPlayerList) {
            PhotonView plyIDPV = PhotonView.Find(plyID);
            Destroy(plyIDPV.gameObject.transform.Find("PropHolder").transform.GetChild(0).gameObject);
            if (plyIDPV.IsMine) { // When we found our player's prop, let's save the viewID.
                myID = plyIDPV.ViewID;
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
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Seeker"), ourPV.gameObject.transform.position, Quaternion.identity, 0, instanceData); //Spawn our ghost prop.
            }
        }

        foreach(int propID in allPropList) {
            if (myID == propID) {
                //We're a pre-prop ghost.
                PhotonView ourPV = PhotonView.Find(myID);
                ourPV.GetComponent<Rigidbody>().isKinematic = true; // Freeze our player, we will unfreeze after prop is spawned, and modified through callback in PropInteraction.
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost"), ourPV.gameObject.transform.position, Quaternion.identity, 0, instanceData); //Spawn our ghost prop.
            }
        }
        Invoke("Invoke_MoveAllToFreshGame", 0.5f);
    }


    //Invokes *********

    // This invoke moves all players into a fresh prep-phase game.
    private void Invoke_MoveAllToFreshGame() {

        //End the loading screen once we're done.
        sController.EndLoadingScreen(2f);
    }

}
