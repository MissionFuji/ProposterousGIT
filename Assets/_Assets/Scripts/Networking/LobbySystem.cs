using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LobbySystem : MonoBehaviourPunCallbacks {


    /* LOBBY IS PRE-ROOM NETWORK */

    public LobbySystem lobby; 
    [SerializeField]
    private GameObject joinRandomRoomButton;
    [SerializeField]
    private GameObject createRoomButton;
    [SerializeField]
    private GameObject cancelSearchButton;
    [SerializeField]
    private GameObject joinWithRoomCodeButton;
    [SerializeField]
    private GameObject optionsButton;
    [SerializeField]
    private GameObject exitRoomButton;
    [SerializeField]
    private GameObject submitRoomCodeButton;
    [SerializeField]
    private GameObject submitRoomCodeInput;
    [SerializeField]
    private GameObject MMUIContainer;
    private PlayerPropertiesController ppc;
    private GameController gc;



    private void Awake() {
        lobby = this;
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
        gc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
    }


    void Start() {
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.SerializationRate = 10;
            PhotonNetwork.SendRate = 18;
        } else {
            Debug.Log("Exception Caught: Tried connecting to master with an active connection already established.");
        }
    }
    
    public override void OnConnectedToMaster() {
        Debug.Log("Connected to Master. Pun 2.0.");
        PhotonNetwork.LocalPlayer.NickName = "TestDummy#" + Random.Range(1, 9999).ToString();
        PhotonNetwork.AutomaticallySyncScene = true;
        MMUIContainer.SetActive(true);
        submitRoomCodeButton.SetActive(false);
        submitRoomCodeInput.SetActive(false);
        joinRandomRoomButton.SetActive(true);
        joinWithRoomCodeButton.SetActive(true);
        createRoomButton.SetActive(true);
        optionsButton.SetActive(true);
        exitRoomButton.SetActive(true);
        if (gc) {
            gc.UpdateGameState(0);
        }
    }

    public void OnJoinRandomLobbyButton_clicked() {
        joinRandomRoomButton.SetActive(false);
        joinWithRoomCodeButton.SetActive(false);
        createRoomButton.SetActive(false);
        optionsButton.SetActive(false);
        exitRoomButton.SetActive(false);
        cancelSearchButton.SetActive(true);
        PhotonNetwork.JoinRandomRoom();
        Debug.Log("Button Pressed! (" + joinRandomRoomButton.name + ")");
    }


    public void OnCreateRoomButton_clicked() {
        cancelSearchButton.SetActive(true);
        joinWithRoomCodeButton.SetActive(false);
        createRoomButton.SetActive(false);
        joinRandomRoomButton.SetActive(false);
        optionsButton.SetActive(false);
        exitRoomButton.SetActive(false);
        CreateRoom();
        Debug.Log("Button Pressed! (" + createRoomButton.name + ")");
    }
    public void OnExitButton_clicked() {
        Application.Quit();
        Debug.Log("Button Pressed! (" + exitRoomButton.name + ")");
    }

    public void OnJoinWithRoomCodeButton_clicked() {
        submitRoomCodeInput.SetActive(true);
        submitRoomCodeButton.SetActive(true);
        cancelSearchButton.SetActive(true);
        joinWithRoomCodeButton.SetActive(false);
        createRoomButton.SetActive(false);
        joinRandomRoomButton.SetActive(false);
        optionsButton.SetActive(false);
        exitRoomButton.SetActive(false);
        Debug.Log("Button Pressed! (" + createRoomButton.name + ")");
    }


    public void OnSubmitRoomCodeButton_clicked() {
        cancelSearchButton.SetActive(true);
        string tempRoomCode = submitRoomCodeInput.transform.Find("InputField/Text").gameObject.GetComponent<Text>().text;
        if (tempRoomCode.Length != 6) {
            Debug.LogError("Room code does not equal 6. Invalid Room Code.");
        } else { 
            PhotonNetwork.JoinRoom(tempRoomCode);
        }
        Debug.Log("Button Pressed! (" + joinWithRoomCodeButton.name + ")");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        if (!submitRoomCodeButton.activeSelf) { // checking if we're trying to use a room code or not.
            Debug.Log("Failed to join random room. (Possibly no rooms to join.)");
            CreateRoom();
        } else {
            Debug.LogError("Failed to join room with that room code.");

        }
    }

    private void CreateRoom() {
        string charList = "asdfghjklnmbvcxzqwertyuiop";
        string seudoRoomName = "";
        int lengthOfCode = 6;
        for (int i = 0; i < lengthOfCode; i++) {
            char c = charList[Random.Range(0, charList.Length)];
            seudoRoomName += c;
        }

        RoomOptions roomOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 10, EmptyRoomTtl = 1 };
        PhotonNetwork.CreateRoom(seudoRoomName.ToUpper(), roomOps);
        Debug.Log("Trying to create new room with roomName: " + seudoRoomName.ToString());
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Tried to create a room with another room name that's already being used. Trying to remake room.");
        CreateRoom();
    }

    public void OnCancelSearchButton_clicked() {
        submitRoomCodeInput.SetActive(false);
        submitRoomCodeButton.SetActive(false);
        cancelSearchButton.SetActive(false);
        createRoomButton.SetActive(true);
        joinRandomRoomButton.SetActive(true);
        joinWithRoomCodeButton.SetActive(true);
        optionsButton.SetActive(true);
        exitRoomButton.SetActive(true);
        PhotonNetwork.LeaveRoom();
        Debug.Log("Button Pressed! (" + cancelSearchButton.name + ")");
    }


}
