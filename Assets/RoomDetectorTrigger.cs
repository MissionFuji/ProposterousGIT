using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class RoomDetectorTrigger : MonoBehaviour
{


    //----------------<THIS SCRIPT ON EACH ROOM TRIGGER OBJECT>----------------\\

    [SerializeField]
    private int objectiveNum;
    [SerializeField]
    private List<GameObject> listOfPlayerObjectsInThisRoom = new List<GameObject>(); // List of objects in THIS room.

    private ObjectiveManager oManager;


    private void Awake() {
        oManager = GameObject.Find("Map").GetComponent<ObjectiveManager>();
        InvokeRepeating("Invoke_CheckIfStillInRoom", 0.1f, 1f);
    }

    private void OnTriggerEnter(Collider other) {
        if (oManager != null) {
            // Making sure we that the collision happened with a player "prop".
            if (other.gameObject.tag == "AttachedProp" && other.gameObject.transform.parent != null) {
                PhotonView enteringPlayerPV = other.gameObject.GetPhotonView();
                // Are we local and are we who we say we are?
                if (enteringPlayerPV != null && enteringPlayerPV.IsMine && enteringPlayerPV.Owner.IsLocal) {
                    // Add entering player to insideRoom list.
                    listOfPlayerObjectsInThisRoom.Add(other.gameObject);
                    // Send a message to oManager to start this objective for us.
                    oManager.TryStartRoomObjective(objectiveNum);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (oManager != null) {
            // Making sure we that the collision happened with a player "prop".
            if (other.gameObject.tag == "AttachedProp" && other.gameObject.transform.parent != null) {
                PhotonView enteringPlayerPV = other.gameObject.GetPhotonView();
                // Are we local and are we who we say we are?
                if (enteringPlayerPV != null && enteringPlayerPV.IsMine && enteringPlayerPV.Owner.IsLocal) {
                    // Remove leaving player from insideRoom list.
                    listOfPlayerObjectsInThisRoom.Remove(other.gameObject);
                    // Send a message to our oManager to cancel the objective for us.
                    oManager.TryCancelRoomObjective(objectiveNum);
                }
            }
        }
    }



}
