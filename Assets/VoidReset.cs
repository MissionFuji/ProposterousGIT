using Photon.Pun;
using UnityEngine;

public class VoidReset : MonoBehaviour
{

    private PhotonView voidPV;

    private void Start() {
        voidPV = gameObject.GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.GetComponent<PropInteraction>()) {
            if (other.gameObject.transform.parent != null) { //Let's see if it has a parent. If not, it's a prop. If so, it's a player.
                voidPV.RPC("RPC_VoidResetPosition", RpcTarget.AllBuffered, other.gameObject.transform.parent.transform.parent.gameObject.GetComponent<PhotonView>().ViewID);
            } else {
                voidPV.RPC("RPC_VoidResetPosition", RpcTarget.AllBuffered, other.gameObject.GetComponent<PhotonView>().ViewID);
            }
        }
    }
    
    [PunRPC]
    private void RPC_VoidResetPosition(int viewID) {
        PhotonView fallingProp = PhotonView.Find(viewID);
        Debug.LogWarning(fallingProp.gameObject.name + " has fallen off of the map. Reseting position over network.");
        fallingProp.gameObject.transform.position = new Vector3(0f,7f,0f);
    }

}
