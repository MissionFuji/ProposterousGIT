using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NameTagHolder : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunOwnershipCallbacks {
    public int ownerID = -1;
    private int actorID = -1;
    public GameObject tarPlayer = null;

    // Update is called once per frame
    void Update()
    {
        if (tarPlayer != null) {
            if (ownerID == -1) {
                ownerID = tarPlayer.GetComponent<PhotonView>().ViewID;
                actorID = tarPlayer.GetComponent<PhotonView>().Owner.ActorNumber;
            }
            gameObject.transform.position = tarPlayer.transform.position + new Vector3(0, tarPlayer.transform.localScale.y + 1f, 0);
        }
    }

    void IPunOwnershipCallbacks.OnOwnershipRequest(PhotonView targetView, Player requestingPlayer) {
        
    }

    void IPunOwnershipCallbacks.OnOwnershipTransfered(PhotonView targetView, Player previousOwner) {

        //The only reason the ownership of these nametags would change, is if the owning player disconnected. Sooo... DESTROY.
        Debug.Log("Destroyed " + previousOwner.NickName + "'s " + gameObject.name + " GameObject over the network.");
        Destroy(gameObject);

    }
}
