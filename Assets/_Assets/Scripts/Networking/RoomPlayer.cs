using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.IO;
using TMPro;

public class RoomPlayer : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunInstantiateMagicCallback {

    private PhotonView pv;
    private PlayerPropertiesController ppc;
    private TextMeshProUGUI nameText;
    private GameObject localPlayerNameTag;

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info) { //This allows us to access our player's "P" prefabs from the PhotonPlayer level. (the level with no viewID.)
        info.Sender.TagObject = this.gameObject;
    }

    private void Awake() {
        pv = gameObject.GetComponent<PhotonView>();
        if (pv.IsMine) {
            gameObject.tag = "LocalPlayer";
            gameObject.layer = 0;
            ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
            ppc.LocalRoomPlayer = gameObject.GetComponent<RoomPlayer>();
            localPlayerNameTag = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "NameTagHolder"), gameObject.transform.position, Quaternion.identity, 0);
            photonView.RPC("RPC_SetNameTagTarget", RpcTarget.AllBuffered, localPlayerNameTag.GetPhotonView().ViewID, pv.ViewID);
        } else {
            gameObject.tag = "ClientPlayer";
            gameObject.layer = 13;
            gameObject.transform.Find("3PC").gameObject.SetActive(false);
        }


        if (pv.IsMine) {
            if (pv.Owner.IsMasterClient) {
                ppc.HostConnected(pv.ViewID);
                Debug.Log("Player recognized by network. We are the Host.");
            } else {
                ppc.ClientConnected(pv.ViewID);
                Debug.Log("Player recognized by network. We are a client.");
            }
        }
    }



    private void SetName() => nameText.text = photonView.Owner.NickName;

    [PunRPC]
    private void RPC_SetNameTagTarget(int nameTagID, int plyID) {
        PhotonView tagPV = PhotonView.Find(nameTagID);
        PhotonView plyPV = PhotonView.Find(plyID);
        tagPV.gameObject.GetComponent<NameTagHolder>().tarPlayer = plyPV.gameObject;


        if (!plyPV.IsMine) {
            nameText = tagPV.gameObject.transform.Find("Canvas_NameTag/Text").gameObject.GetComponent<TextMeshProUGUI>();
            SetName();
        }

    }

}
