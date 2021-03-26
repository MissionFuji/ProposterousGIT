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

    [SerializeField]
    private int percentToCompleteHaunt = 0;




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
            sController.AddToHauntBar(Random.Range(0.05f, 0.15f));
        }

        if (percentToCompleteHaunt >= 100) {
            if (PhotonNetwork.IsMasterClient) {
                gController.UpdateGameplayState(4);
                Debug.Log("TEMPORARY: Haunt Value reached max. Ghosts win.");
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

    [PunRPC]
    private void RPC_AddToHauntCounter(int hauntVal) {
        percentToCompleteHaunt += hauntVal;

        float result = hauntVal * 0.01f; // This turns our int into a %.
        sController.AddToHauntBar(result);
    }


}
