using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour {


    // Regular Private Vars
    private ScreenController sController;
    private GameplayController gController;
    private GameObject localPlayerRoot;
    private PhotonView omPV;
    private bool GameFinished = false;

    [SerializeField]
    private int percentToCompleteHaunt = 0;
    [SerializeField]
    private List<HauntInteraction> listOfHauntInts = new List<HauntInteraction>();




    private void Awake() {
        // Controller References
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();

        // Reference to our localPlayer object.
        localPlayerRoot = (GameObject)PhotonNetwork.LocalPlayer.TagObject;

        // Reference to our PhotonView on this object.
        omPV = GetComponent<PhotonView>();
    }

    private void Start() {
        sController.DisplayHauntBar(true);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.L)) {
            AddToHauntCounter(Random.Range(3, 25));
        }

        if (percentToCompleteHaunt >= 100) {
            if (PhotonNetwork.IsMasterClient) {
                if (!GameFinished) {
                    gController.UpdateGameplayState(4);
                    GameFinished = true;
                    Debug.Log("TEMPORARY: Haunt Value reached max. Ghosts win.");
                }
            }
        }
    }

    // Only our masterclient and influence this.
    public void AddToHauntCounter(int hauntVal) {
        // Make sure the MC is the only one who can update this for all clients.
        if (PhotonNetwork.IsMasterClient) {
            omPV.RPC("RPC_AddToHauntCounter", RpcTarget.AllBuffered, hauntVal);
        }
    }

    // Ran by the master client when we open the gate and when the activeGame phase begins.
    public void InitializeHauntInteractions() {
        if (PhotonNetwork.IsMasterClient) {
            omPV.RPC("RPC_ActivateHauntInteractionsOverNetwork", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void RPC_AddToHauntCounter(int hauntVal) {
        percentToCompleteHaunt += hauntVal;

        float result = hauntVal * 0.01f; // This turns our int into a %.
        sController.AddToHauntBar(result);
    }

    // Runs on all clients. Sent from masterclient because we don't want client players running this public function via cheating.
    [PunRPC]
    private void RPC_ActivateHauntInteractionsOverNetwork() {

        // Our counter.
        int hiCounter = 0;

        // Interate through our preset list of haunt interaction objects.
        foreach (HauntInteraction hi in listOfHauntInts) {
            // Enable if disabled.
            if (hi.enabled == false) {
                hi.enabled = true;
            }

            // Count iteration.
            hiCounter++;


        }
    }


}
