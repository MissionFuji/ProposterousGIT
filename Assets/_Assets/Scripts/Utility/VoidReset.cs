using Photon.Pun;
using Photon.Realtime;
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
                if (other.gameObject.transform.parent != null) { //Let's see if it's an attached prop. (Prop, ghost, seeker OR pre-prop.)
                    voidPV.RPC("RPC_VoidResetPosition", RpcTarget.AllBuffered, other.gameObject.transform.root.GetComponent<PhotonView>().ViewID);
                } else { // Unattached prop.

                    // Must set KOS tag in order to be deleted over the network.
                    other.gameObject.tag = "KOS";

                    // Transfer ownership to the MC.
                    other.gameObject.GetPhotonView().TransferOwnership(PhotonNetwork.MasterClient);
                    // OwnershipTransfer Callback in PropInteraction will handle the deletion of this object.
                }
            } else {// No PropInteraction
                if (!other.gameObject.GetComponent<PhotonView>()) { // No PhotonView
                    Destroy(other.gameObject);
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
