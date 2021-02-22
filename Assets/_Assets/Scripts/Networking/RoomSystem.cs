using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


public class RoomSystem : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    /* ROOM IS PRE-GAME NETWORK */

    //Room Information
    public static RoomSystem room;
    private PhotonView pv;
    private PlayerPropertiesController ppc;
    private GameplayController gController;
    private ScreenController sController;
    private CameraController cController;
    public bool isGameLoaded;
    public int currentScene;
    public int multiplayerScene;

    public GameObject MMUI;
    public GameObject RoomUI;
    private Text RoomCode;
    private Text PlayerCounter;

    [SerializeField]
    private List<GameObject> spawnPositions = new List<GameObject>();
    [SerializeField]
    private Vector3 offset;

    //Player Info
    public int playersInRoom;
    public int myNumberInRoom;
    public int playersInGame;


    private int numOfPlayersSpawnedIn;


    private void Awake() {
        //Singleton
        if (RoomSystem.room == null) {
            RoomSystem.room = this;
        } else {
            if (RoomSystem.room != this) {
                Destroy(RoomSystem.room.gameObject);
                RoomSystem.room = this;
            }
        }
        DontDestroyOnLoad(this.gameObject);
        pv = gameObject.GetComponent<PhotonView>();
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        cController = Camera.main.GetComponent<CameraController>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
        foreach (Transform child in transform) {
            spawnPositions.Add(child.gameObject);
        }
        InvokeRepeating("UpdateRoomCodeAndPlayerCounter", 0.1f, 0.5f);
    }



    void CreateRoomPlayer() {
        int r = Random.Range(0, spawnPositions.Count - 1);
        if (PhotonNetwork.LocalPlayer.IsLocal) {
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "P"), spawnPositions[r].transform.position, Quaternion.identity, 0);
            photonView.RPC("RPC_UpdatePlayerSpawnedInCount", RpcTarget.MasterClient, 1);
            sController.EndLoadingScreen(1f); // Ending the loadingscreen that's up, if there is one up.
        }
    }

    public override void OnEnable() {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable() {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // When LOCAL PLAYER joins the room.
    public override void OnJoinedRoom() {
        base.OnJoinedRoom();
        if (!PhotonNetwork.LocalPlayer.IsMasterClient) { // If we're a regular Client.
            if (PhotonNetwork.LocalPlayer.IsLocal) {
                gController.EnableMainMenuPrefab(false);
                CreateRoomPlayer();
            }
        } else { // If we're the host.
            gController.EnableMainMenuPrefab(false);
            gController.UpdateGameplayState(1);
            CreateRoomPlayer();
        }
        ppc.moveState = 1; // Make sure we go back to "pre-prop" movestate.
        Debug.Log("Successfully joined a room: " + PhotonNetwork.CurrentRoom.Name + ", as player: " + PhotonNetwork.LocalPlayer.NickName + ".");
        MMUI.SetActive(false);
        RoomUI.SetActive(true);
    }

    // When OTHER players enter.
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log(newPlayer.NickName + " has entered this lobby.");
    }

    // Local Player Left room.
    public override void OnLeftRoom() {
        Debug.Log("Successfully Left a room.");
        RoomUI.SetActive(false);
        MMUI.SetActive(true); // Show main menu again.
        gController.UpdateGameplayState(0);
        sController.EndLoadingScreen(2f);
        cController.ReadyCamera(transform, false);
        base.OnLeftRoom();
    }

    // When OTHER players leave.
    public override void OnPlayerLeftRoom(Player leavingPlayer) {
        if (PhotonNetwork.CurrentRoom != null) {
            if (!leavingPlayer.IsMasterClient) {
                photonView.RPC("RPC_UpdatePlayerSpawnedInCount", RpcTarget.MasterClient, -1);
            } else {
                //RPC To kick everyone here
                photonView.RPC("RPC_HostLeftCloseRoom", RpcTarget.Others);
            }
            base.OnPlayerLeftRoom(leavingPlayer);
        }
        Debug.Log("Player: " + leavingPlayer.NickName + " just left the game.");
    }


    [PunRPC]
    void RPC_HostLeftCloseRoom() {
        if (PhotonNetwork.CurrentRoom != null) {
            PhotonNetwork.LeaveRoom();
            Debug.Log("Force to leave room because host left the game.");
        }
    }

    [PunRPC]
    void RPC_UpdatePlayerSpawnedInCount(int dif) {
        numOfPlayersSpawnedIn = numOfPlayersSpawnedIn + dif;
    }

    void UpdateRoomCodeAndPlayerCounter() {
        if (RoomUI.activeSelf) {
            if (RoomCode == null) {
                RoomCode = RoomUI.transform.Find("RoomCode").gameObject.GetComponent<Text>();
            }
            if (PlayerCounter == null) {
                PlayerCounter = RoomUI.transform.Find("PlayerCounter").gameObject.GetComponent<Text>();
            }
            if ((RoomCode.text != PhotonNetwork.CurrentRoom.Name) || (PlayerCounter.text != PhotonNetwork.CurrentRoom.PlayerCount.ToString())) {
                RoomCode.text = PhotonNetwork.CurrentRoom.Name;
                PlayerCounter.text = PhotonNetwork.CurrentRoom.PlayerCount.ToString() + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
            }

        }
    }



}
