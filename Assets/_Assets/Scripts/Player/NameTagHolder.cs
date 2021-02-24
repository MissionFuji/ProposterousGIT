using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NameTagHolder : MonoBehaviourPunCallbacks, IInRoomCallbacks {
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
        } else {
            if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(actorID)) {
                Destroy(gameObject);
            } else {
                Debug.LogError("Player is null but is still found in the game via Actor#?");
            }
        }
    }
}
