using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NameTagHolder : MonoBehaviourPunCallbacks, IInRoomCallbacks {
    public int ownerID = -1;
    private int actorID = -1;
    public GameObject tarPlayer = null;
    [SerializeField]
    private LayerMask layerToHit;
    [SerializeField]
    private Vector3 nameTagOffset;

    // Update is called once per frame
    void Update()
    {
        if (tarPlayer != null) {
            if (ownerID == -1) {
                ownerID = tarPlayer.GetComponent<PhotonView>().ViewID;
                actorID = tarPlayer.GetComponent<PhotonView>().Owner.ActorNumber;
            }

            RaycastHit hit;
            Vector3 originPoint = new Vector3(tarPlayer.transform.position.x, tarPlayer.transform.position.y + 100f, tarPlayer.transform.position.z);
            // Are we hitting prop Interaction Layer? Are we also hitting a prop interaction layer on our TARGET client player?
            if (Physics.Raycast(originPoint, -gameObject.transform.up, out hit, 200f, layerToHit) && (hit.collider.gameObject.transform.root == tarPlayer.transform)) {
                gameObject.transform.position = hit.point + nameTagOffset;
                Debug.Log("TESTING NAMETAG HOLDER: " + hit.collider.transform.root.gameObject.name);
            } else {
                gameObject.transform.position = tarPlayer.transform.position + nameTagOffset;
                Debug.Log("NAMETAG BROKEN HOLDER POSITION BROKEN");
            }
        }
    }
}
