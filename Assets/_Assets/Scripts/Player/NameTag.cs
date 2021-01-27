using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class NameTag : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    private TextMeshProUGUI nameText;


    // Start is called before the first frame update
    void Start() {
        if (!photonView.IsMine) {
            SetName();
        }
    }


    // Update is called once per frame
    void Update() {

    }


    private void SetName() => nameText.text = photonView.Owner.NickName;

}
