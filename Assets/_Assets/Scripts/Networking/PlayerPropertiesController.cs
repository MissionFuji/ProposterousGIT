using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerPropertiesController : MonoBehaviourPunCallbacks, IInRoomCallbacks {
    public RoomPlayer LocalRoomPlayer;
    public int moveState = 1; // 1 preProp(Ghost), 2 propMove, 3 seekerMove, 4 spectator(DeadGhost)
    public bool playerIsFrozen = false;
    private ScreenController sController;
    private GameplayController gController;
    private PhotonView pv;


    private void Awake() {
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();
        pv = gameObject.GetComponent<PhotonView>();
    }


    // ALL OF THESE ARE LOCALPLAYER
    public void HostConnected(int plyID) {
        
    }

    public void ClientConnected(int plyID) {
        photonView.RPC("RPC_ClientConnected", RpcTarget.MasterClient, PhotonView.Find(plyID).Owner.NickName, plyID);
    }

    public void ClientDisconnecting(int plyID) {
        photonView.RPC("RPC_ClientDisconnected", RpcTarget.MasterClient, PhotonView.Find(plyID).Owner.NickName, plyID);
        if (PhotonNetwork.CurrentRoom != null) {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void HostDisconnecting(int plyID) {

        // Tell all other players to leave before I disconnect.
        photonView.RPC("RPC_RemovePlayer", RpcTarget.Others);
        if (PhotonNetwork.CurrentRoom != null) { // If i'm in a room.
            PhotonNetwork.LeaveRoom();
        }
    }
    // END OF LOCALPLAYER

    //RPC's *******

    [PunRPC]
    private void RPC_ClientConnected(string plyName, int plyID) {
        Debug.Log("Player joined the lobby with name: " + plyName);
    }


    [PunRPC]
    private void RPC_ClientDisconnected(string plyName, int plyID) {
        gController.MasterClientRemovesPlayerFromListOnDisconnect(plyID);
        Debug.Log("Player left the lobby with name: " + plyName);
    }

    [PunRPC]
    private void RPC_RemovePlayer() {
        sController.RunLoadingScreen(2);
        Invoke("Invoke_RemovePlayer", 0.5f);
    }


    //Invokes ******
    private void Invoke_RemovePlayer() {
        if (PhotonNetwork.CurrentRoom != null) {
            PhotonNetwork.LeaveRoom();
            Debug.Log("Forced To LeaveRoom By Host.");
        }
    }

    

}
