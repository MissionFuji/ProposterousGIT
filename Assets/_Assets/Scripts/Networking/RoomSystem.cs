using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class RoomSystem : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    /* ROOM IS PRE-GAME NETWORK */

    //Room Information
    public static RoomSystem room;
    private PhotonView pv;
    private PlayerPropertiesController ppc;
    public bool isGameLoaded;
    public int currentScene;
    public int multiplayerScene;

    public GameObject MMUI;
    public GameObject RoomUI;
    private Text RoomCode;
    private Text PlayerCounter;
    private MainMenuCamera MMCam;
    private GameController gc;
 



    [SerializeField]
    private List<GameObject> spawnPositions = new List<GameObject>();
    [SerializeField]
    private Vector3 offset;

    //Player Info
    Player[] photonPlayer;
    public int playersInRoom;
    public int myNumberInRoom;

    public int playersInGame;


    private int numOfPlayersSpawnedIn;

    private bool isFirstLoad = true;


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
        MMCam = Camera.main.GetComponent<MainMenuCamera>();
        pv = gameObject.GetComponent<PhotonView>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
        gc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        SceneManager.sceneLoaded += OnSceneLoaded;
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
            Debug.Log("CreateRoomPlayer Ran for: " + PhotonNetwork.NickName);
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { //This was done this way when I was trying to have multiple scenes. We're moving to single-scene strategy now.

        if (!isFirstLoad) {
            Debug.Log("SCENE LOADER: Loaded into scene: " + scene.buildIndex.ToString());
            if (!PhotonNetwork.LocalPlayer.IsMasterClient) {
                if (PhotonNetwork.LocalPlayer.IsLocal) {
                    CreateRoomPlayer();
                    Debug.Log(PhotonNetwork.NickName + " loaded into scene. We created out own character again.");
                }
            } else {
                Debug.Log("You are the host.");
                CreateRoomPlayer();
            }
        } else {
            isFirstLoad = false;
        }

    }

    // When LOCAL PLAYER joins the room.
    public override void OnJoinedRoom() {
        base.OnJoinedRoom();
        gc.UpdateGameState(1);
        if (!PhotonNetwork.LocalPlayer.IsMasterClient) {
            if (PhotonNetwork.LocalPlayer.IsLocal) {
                CreateRoomPlayer();
                Debug.Log(PhotonNetwork.NickName + " joined the room. We created out own character.");
            }
        } else {
            Debug.Log("You are the host.");
            CreateRoomPlayer();
        }
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
        base.OnLeftRoom();
        if (gc) {
            gc.UpdateGameState(0);
        }
    }

    // When OTHER players leave.
    public override void OnPlayerLeftRoom(Player leavingPlayer) {
        if (PhotonNetwork.CurrentRoom != null) {
            if (!leavingPlayer.IsMasterClient) {

            } else {
                //RPC To kick everyone here
                photonView.RPC("RPC_HostLeftCloseRoom", RpcTarget.Others);
            }
            base.OnPlayerLeftRoom(leavingPlayer);
        }
        Debug.Log("Player: " + leavingPlayer.NickName + " just left the game.");
        PhotonNetwork.DestroyPlayerObjects(leavingPlayer); // Destroys player's items and char.
        photonView.RPC("RPC_UpdatePlayerSpawnedInCount", RpcTarget.MasterClient, -1);
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
        Debug.Log("Number of players spawned in successfully: " + numOfPlayersSpawnedIn.ToString() + "Number of players in PhotonNetwork Room: " + PhotonNetwork.CurrentRoom.PlayerCount.ToString());
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
