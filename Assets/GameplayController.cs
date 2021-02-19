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

    private PhotonView gcpv;
    private ScreenController sController;
    private GameObject currentMapLoaded; //This is the current map prefab loaded. Could be pre-game lobby, office map, candy land map, etc.



    private void Awake() {
        gcpv = GetComponent<PhotonView>();
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
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

    private void MoveAllToPreGameLobby() {
        // We disable MainMenuProp in RoomSystem when the OnJoined Callback is ran.
        currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PreGameLobby"), Vector3.zero, Quaternion.identity, 0);
    }

    private void MoveAllToFreshGame() {
        int loadingScreenRoutine = 2;
        gcpv.RPC("RPC_MoveAllToFreshGame", RpcTarget.All, loadingScreenRoutine); //Don't want to buffer this one i don't think.
    }

    //RPC's *********
    [PunRPC]
    private void RPC_MoveAllToFreshGame(int loadingScreenRoutine) {
        sController.RunLoadingScreen(loadingScreenRoutine);
        Invoke("Invoke_MoveAllToFreshGame", 0.5f);
    }


    //Invokes *********

    private void Invoke_MoveAllToFreshGame() {
        if (PhotonNetwork.IsMasterClient) { // Only if we're host to we spawn the map and destroy the old one over the network.
            if (currentMapLoaded != null) {
                PhotonNetwork.Destroy(currentMapLoaded);
                int r = Random.Range(0, mapList.Count - 1);
                currentMapLoaded = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", mapList[r].name), Vector3.zero, Quaternion.identity, 0);
            }
        }
        sController.EndLoadingScreen(2f);
    }

}
