using Photon.Pun;
using UnityEngine;

public class StartGameDevTool : MonoBehaviour
{

    private GameplayController gController;

    private void Awake() {
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.transform.parent.transform.parent.gameObject.tag == "LocalPlayer") { // Did WE touch it? Edit: Jesus christ that string of transform jumps is silly.
            if (PhotonNetwork.IsMasterClient) { // Are we the host?
                if (PhotonNetwork.CurrentRoom.PlayerCount > 1) { //There must be atleast to player in a room to start the game.
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                    PhotonNetwork.CurrentRoom.IsVisible = false;
                    gController.UpdateGameplayState(2);
                }
            }
        }
    }

}
