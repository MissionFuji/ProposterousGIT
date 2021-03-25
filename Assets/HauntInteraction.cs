using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HauntInteraction : MonoBehaviour
{

    public bool canHaunt = true;

    [SerializeField]
    private int hauntValue;
    [SerializeField]
    private int hauntCooldown;

    private PhotonView hiPV;
    private ObjectiveManager oMgr;
    private int remainingHauntCooldown;

    private void Awake() {
        hiPV = GetComponent<PhotonView>();
        oMgr = GameObject.FindGameObjectWithTag("Map").GetComponent<ObjectiveManager>();
    }

    // Typically run from PlayerMovement when prop interacts with an object via "E".
    // We run this function locally to send this Request to the MC.
    public void TryToTriggerHauntInteraction(int requestingPlyID) {
        // Sort out exactly who's making this request.
        PhotonView reqPly = PhotonView.Find(requestingPlyID);
        // Are we who we say we are?
        if (reqPly != null && reqPly.IsMine && reqPly.Owner.IsLocal) {
            hiPV.RPC("RPC_SendHauntRequestToMasterClient", RpcTarget.MasterClient);
        } else {
            Debug.Log("Haunt interaction failed. Ply was null or not mine.");
        }
    }
    
    // Run this on MasterClient only.
    [PunRPC]
    private void RPC_SendHauntRequestToMasterClient() {
        if (PhotonNetwork.IsMasterClient) {
            // Is object active and ready for haunt? 
            if (canHaunt == true) {
                // Object is ready, time to accept the request and start the cooldown.
                hiPV.RPC("RPC_AcceptHauntRequest", RpcTarget.AllBuffered);
            } else {
                Debug.LogWarning("Request to haunt object received, however it look like it's already been haunted. Ignoring request.");
            }
        }
    }

    // Run this on every client.
    [PunRPC]
    private void RPC_AcceptHauntRequest() {
        Haunt();
    }

    // Run this on every client.
    [PunRPC]
    private void RPC_HauntInteractionCooldownEnded() {
        // Temporarily modify color to show visual cooldown.
        gameObject.GetComponent<Renderer>().material.color = Color.white;

        canHaunt = true;
    }

    private void Haunt() {

        if (oMgr != null) {
            // Temporarily modify color to show visual cooldown.
            gameObject.GetComponent<Renderer>().material.color = Color.red;

            // Prevent further haunting of this object until cooldown is up.
            canHaunt = false;

            // We only run the countdown on the masterclient. They will tell everyone else when cooldown is over.
            if (PhotonNetwork.IsMasterClient) {
                // Reset our timer.
                remainingHauntCooldown = hauntCooldown;
                // Start our timer.
                InvokeRepeating("Invoke_HauntCooldownTick", 0.0f, 1.0f);
            }

            // Send over the haunt value to the objective manager.
            oMgr.AddToHauntCounter(hauntValue);

        } else {
            Debug.LogError("oMgr reference is null. Can't finalize haunt interaction.");
        }

    }

    private void Invoke_HauntCooldownTick() {
        remainingHauntCooldown--;
        if (remainingHauntCooldown == 0) {
            CancelInvoke("Invoke_HauntCooldownTick");
            hiPV.RPC("RPC_HauntInteractionCooldownEnded", RpcTarget.AllBuffered);
        }
    }

}
