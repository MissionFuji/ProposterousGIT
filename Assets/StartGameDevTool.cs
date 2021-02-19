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
                gController.UpdateGameplayState(2);
            }
        }
    }

}
