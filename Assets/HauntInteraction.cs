using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HauntInteraction : MonoBehaviour
{

    [Header("HauntInteraction Settings:")]
    [Tooltip("Initial haunt value to add towards hauntBar.")]
    [SerializeField]
    private int hauntValue;
    [Tooltip("Incremental haunt value to add over time toward hauntBar.")]
    [SerializeField]
    private int hauntOverTimeValue;
    [Tooltip("How long is the cooldown after repair?")]
    [SerializeField]
    private int hauntCooldown;
    [Tooltip("Will this object Haunt-Over-Time until fixed? (HoT)")]
    [SerializeField]
    private bool hauntOverTime;

    private int hauntState = 0; // 0 Ready To Haunt, 1 Haunted; Waiting to be fixed, 2 Fixed, waiting on cooldown to end.

    private PhotonView hiPV;
    private ObjectiveManager oMgr;
    private int remainingHauntCooldown;

    private void Start() {
        hiPV = GetComponent<PhotonView>();
        oMgr = GameObject.FindGameObjectWithTag("Map").GetComponent<ObjectiveManager>();
        hauntState = 0;
    }

    public int GetState() {
        return hauntState;
    }

    // Haunt.
    #region


    // Typically run from PlayerMovement when prop HAUNTS with an object via "E".
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
            if (hauntState == 0) {
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


    private void Haunt() {

        if (oMgr != null) {

            // Temporarily modify color to show visual cooldown.
            gameObject.GetComponent<Renderer>().material.color = Color.red;

            // Set HauntInteraction State to prevent further haunting of this object.
            hauntState = 1;

            // Send over the haunt value to the objective manager.
            oMgr.AddToHauntCounter(hauntValue);

            // If enabled, start the Haunt Over Time effect.
            if (hauntOverTime) {
                InvokeRepeating("Invoke_HauntOverTimeTick", 1.0f, 1.0f);
            }

        } else {
            Debug.LogError("oMgr reference is null. Can't finalize haunt interaction.");
        }

    }

    #endregion

    // Repair.
    #region
    // Typically run from PlayerMovement when seeker REPAIRS haunted prop via "E".
    // We run this function locally to send this Request to the MC.
    public void TryToRepairHauntInteraction(int fixingPlyID) {

        // Sort out exactly who's making this request.
        PhotonView fixingPly = PhotonView.Find(fixingPlyID);

        // Are we who we say we are?
        if (fixingPly != null && fixingPly.IsMine && fixingPly.Owner.IsLocal) {
            hiPV.RPC("RPC_SendRepairRequestToMasterClient", RpcTarget.MasterClient);
        } else {
            Debug.Log("Haunt repair failed. Ply was null or not mine.");
        }

    }

    // Run this on MasterClient only.
    [PunRPC]
    private void RPC_SendRepairRequestToMasterClient() {
        if (PhotonNetwork.IsMasterClient) {
            // Is object active and ready for repair? 
            if (hauntState == 1) {
                // Object is ready, time to accept the request and start the cooldown.
                hiPV.RPC("RPC_AcceptRepairRequest", RpcTarget.AllBuffered);
            } else {
                Debug.LogWarning("Request to repair haunt object received, however it look like it's already been repaired. Ignoring request.");
            }
        }
    }

    // Run this on every client.
    [PunRPC]
    private void RPC_AcceptRepairRequest() {
        Repair();
    }

    private void Repair() {

        // Temporarily modify color to show visual cooldown.
        gameObject.GetComponent<Renderer>().material.color = Color.blue;

        // Reset our timer.
        remainingHauntCooldown = hauntCooldown;

        // Start our cooldown timer.
        InvokeRepeating("Invoke_HauntCooldownTick", 0.0f, 1.0f);

        // Stop our haunt-over-time tick.
        CancelInvoke("Invoke_HauntOverTimeTick");

    }

    #endregion

    // This runs every second after a haunted prop is repaired. After countdown ends, back to normal.
    private void Invoke_HauntCooldownTick() {

        // Counts down each second.
        remainingHauntCooldown--;

        // Check if we're at zero yet.
        if (remainingHauntCooldown == 0) {

            // Zero reached, cooldown is finished. Time reset haunted prop.
            hiPV.RPC("RPC_HauntStateReset", RpcTarget.AllBuffered);

        }

    }


    // This runs every second after a prop is haunted AND the hauntOverTime bool is true.
    private void Invoke_HauntOverTimeTick() {

        // Add HoT value to hauntBar.
        oMgr.AddToHauntCounter(hauntOverTimeValue);
        
        // Check if haunted prop was fixed unexpectedly? Just in-case.
        if (hauntState == 0) {
            CancelInvoke("Invoke_HauntOverTimeTick");
        }

    }


    // Run this on every client. Resets HauntInteraction prop state.
    [PunRPC]
    private void RPC_HauntStateReset() {

        // Cancel our cooldown timer.
        CancelInvoke("Invoke_HauntCooldownTick");

        // Temporarily modify color to show visual cooldown.
        gameObject.GetComponent<Renderer>().material.color = Color.white;

        // Reset our variables.
        hauntState = 0;

    }

}
