using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NameTagHolder : MonoBehaviourPunCallbacks, IInRoomCallbacks {
    [SerializeField]
    private int ownerID = -1;
    public GameObject tarPlayer = null;
    [SerializeField]
    private Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        if (tarPlayer != null) {
            if (ownerID == -1) {
                ownerID = tarPlayer.GetComponent<PhotonView>().ViewID;
            }
            gameObject.transform.position = tarPlayer.transform.position + offset;
        }
    }
}
