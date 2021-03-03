using Photon.Pun;
using UnityEngine;

public class VoidReset : MonoBehaviour
{

    private PhotonView voidPV;

    private void Start() {
        voidPV = gameObject.GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other) {
        if (PhotonNetwork.IsMasterClient) {
            if (other.gameObject.GetComponent<PropInteraction>()) {
                if (other.gameObject.tag == "AttachedProp") { //Let's see if it's an attached prop.
                                                              //We get from the prop, to the prop holder, to the player.
                    voidPV.RPC("RPC_VoidResetPosition", RpcTarget.AllBuffered, other.gameObject.transform.parent.transform.parent.gameObject.GetComponent<PhotonView>().ViewID);
                } else { // Unattached prop.
                    PhotonNetwork.Destroy(other.gameObject);
                }
            }
        }
    }
    
    [PunRPC]
    private void RPC_VoidResetPosition(int viewID) {
        PhotonView fallingProp = PhotonView.Find(viewID);
        Debug.LogWarning(fallingProp.gameObject.name + " has fallen off of the map. Resetting position over network.");
        fallingProp.gameObject.transform.position = new Vector3(0f,7f,0f);
    }

}
