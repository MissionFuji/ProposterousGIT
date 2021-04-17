using Photon.Pun;
using UnityEngine;

public class ForcedStartProp : MonoBehaviour
{

    private GameplayController gController;

    //---------------------------<THIS SCRIPT IS PUT ON PROPINTERACTION PROPS THAT ARE PART OF THE MAP PREFAB>---------------------------\\

    private void Awake() {
        if (PhotonNetwork.IsMasterClient) {

            //Set reference of our GameController.
            gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();

        }
    }

    void Start()
    {
        if (gController != null) {
            if (PhotonNetwork.IsMasterClient) {
                gController.TellMasterClientToAddPropToDestroyList(gameObject);
            }
        } else {
            Debug.Log("gController reference never captured or missing.");
        }
    }
}
