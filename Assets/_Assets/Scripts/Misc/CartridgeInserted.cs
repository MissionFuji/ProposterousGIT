using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartridgeInserted : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.GetComponent<PlayerMovement>()) {
            PlayerMovement pm = other.gameObject.GetComponent<PlayerMovement>();
            if (pm.mapToLoadName == "MAP1") {
                Debug.Log("MAP1 LOADED.");

            } else {
                Debug.Log("Cartridge unreadable. (unknown map name). " + pm.mapToLoadName + "..");
            }
            Debug.Log("A host-only prop has been inserted into cartridge slot. Probably a map.");
        }
    }

}
