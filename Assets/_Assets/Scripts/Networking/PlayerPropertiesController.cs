using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerPropertiesController : MonoBehaviourPunCallbacks, IInRoomCallbacks {
    public RoomPlayer LocalRoomPlayer;
    public int moveState = 1; // 1 preProp(Ghost), 2 propMove, 3 seekerMove, 4 spectator(DeadGhost)
    public bool playerIsFrozen = false;
    private PhotonView pv;
    

    // ALL OF THESE ARE LOCALPLAYER
    public void HostConnected(int plyID) {
        
    }

    public void ClientConnected(int plyID) {
        photonView.RPC("RPC_ClientConnected", RpcTarget.MasterClient, plyID);
    }

    public void ClientDisconnecting(int plyID) {
        photonView.RPC("RPC_ClientDisconnected", RpcTarget.MasterClient, plyID);
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


    [PunRPC]
    private void RPC_ClientConnected(int plyID) {
        PhotonView pv = PhotonView.Find(plyID);
        Debug.Log("Player joined the lobby with playerID: " + pv.Owner.NickName);
    }


    [PunRPC]
    private void RPC_ClientDisconnected(int plyID) {
        PhotonView pv = PhotonView.Find(plyID);
            Debug.Log("Player left the lobby with playerID: " + pv.Owner.NickName);
    }

    [PunRPC]
    private void RPC_RemovePlayer() {
        if (PhotonNetwork.CurrentRoom != null) {
            PhotonNetwork.LeaveRoom();
            Debug.Log("Forced To LeaveRoom By Host.");
        }
    }


    private void Awake() {
        pv = gameObject.GetComponent<PhotonView>();
    }



}
