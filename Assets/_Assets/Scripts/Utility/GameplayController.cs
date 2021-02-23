using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Collections.Generic;

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
        gcpv.RPC("RPC_MoveAllToFreshGame", RpcTarget.All, loadingScreenRoutine); //Don't want to buffer this one i don't think.
    }

    //RPC's *********
    //Runs on all clients.
    [PunRPC]
    private void RPC_MoveAllToFreshGame(int loadingScreenRoutine) {
        sController.RunLoadingScreen(loadingScreenRoutine); // Start a loading screen.
        //GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer"); // Reference our localplayer.
        //int myPV = localPlayer.GetPhotonView().ViewID; // Reference our localPlayer's ViewID to send it to MasterClient for PlayerList.
        //gcpv.RPC("RPC_HelpMasterBuildPlayerList", RpcTarget.MasterClient, myPV);
        ppc.moveState = 1; // pre-prop moveState.
        Invoke("Invoke_MoveAllToFreshGame", 0.5f);
    }

    //Each player in room tells master client to run this.
    [PunRPC]
    private void RPC_HelpMasterBuildPlayerList(int plyID) {
        InGamePlayerList.Add(plyID); // Add each player to a list.
        if (InGamePlayerList.Count == PhotonNetwork.CurrentRoom.PlayerCount) { // Does our newly completed list match the PhotonNetwork playerlist?
            foreach (int plyIDToSort in InGamePlayerList) {
                Debug.Log("Are we looping?");
                if (InGamePlayerList.Count < 6) { //If there are 5 players, use one seeker.
                    Debug.Log("Are there less than 6 of us?");
                    if (SeekerPlayerList.Count < 1) {
                        SeekerPlayerList.Add(plyIDToSort); // Add our seekers to the seeker list.
                        Debug.Log("Add a seeker?");
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
                }
            }
            Debug.Log("Finished making our customer Player ViewID List.");
        } 
    }


    //Invokes *********

    // This invoke moves all players into a fresh prep-phase game.
    private void Invoke_MoveAllToFreshGame() {
        if (PhotonNetwork.IsMasterClient) { // Only if we're host to we spawn the map and destroy the old one over the network.
            if (currentMapLoaded != null) {
                PhotonNetwork.Destroy(currentMapLoaded);
                int r = Random.Range(0, mapList.Count - 1);
                currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", mapList[r].name), Vector3.zero, Quaternion.identity, 0);
            }
        }

        //Passing 0 = PropSpawner. Passing 1 = Player-takover spawn. Passing 2 = Player Becoming Ghost/Seeker.
        //We must send instantiation data with object when we spawn it. We do this to determine if it was spawned by a player, or by a prop-spawner.
        object[] instanceData = new object[1];
        instanceData[0] = 2;

        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer"); // Reference our localplayer.
        if (localPlayer != null) {
                PhotonNetwork.Destroy(localPlayer.transform.Find("PropHolder").transform.GetChild(0).gameObject); //We destroy our prop before we move to the new pre-phase map.
                GameObject newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player_Ghost"), localPlayer.transform.position, localPlayer.transform.rotation, 0, instanceData); //Spawn our ghost prop.
        }
        //End the loading screen once we're done.
        sController.EndLoadingScreen(2f);
    }

}
