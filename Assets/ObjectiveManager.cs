using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour {


    // Regular Private Vars
    private ScreenController sController;
    private GameplayController gController;
    private GameObject localPlayerRoot;

    [SerializeField]
    private float percentToCompleteHaunt = 0f;




    private void Awake() {
        // Controller References
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();

        // Reference to our localPlayer object.
        localPlayerRoot = (GameObject)PhotonNetwork.LocalPlayer.TagObject;
    }

    private void Start() {
        sController.DisplayHauntBar(true);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.L)) {
            sController.AddToHauntBar(Random.Range(0.05f, 0.15f));
        }
    }

    // Only our masterclient and influence this.
    public void AddToHauntCounter(int hauntVal) {
        if (PhotonNetwork.IsMasterClient) {

        }
    }


}
