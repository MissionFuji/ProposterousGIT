using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class RoomDetectorTrigger : MonoBehaviour
{


    //----------------<THIS SCRIPT ON EACH ROOM TRIGGER OBJECT>----------------\\

    [Header("RoomDetectorTrigger Settings")]
    [Tooltip("The objective to complete.")]
    [SerializeField]
    private int objectiveNum;
    [Tooltip("The required prop needed in order to complete.")]
    [SerializeField]
    private int requiredPropID;
    [SerializeField]
    private List<GameObject> listOfPlayerObjectsInThisRoom = new List<GameObject>(); // List of objects in THIS room.

    private ObjectiveManager oManager;


    private void Awake() {
        oManager = GameObject.FindGameObjectWithTag("Map").GetComponent<ObjectiveManager>();
    }

    private void OnTriggerEnter(Collider other) {
        if (oManager != null) {
            // Making sure we that the collision happened with a player "prop".
            if (other.gameObject.tag == "AttachedProp" && other.gameObject.transform.parent != null) {

                // Get a reference to the PI on the touching object.
                PropInteraction propPI = other.GetComponent<PropInteraction>();

                // Get another reference of our intial propID in-case it changes.
                int initialPropID = propPI.GetPropID();

                // Check if its got the required prop ID.
                if (initialPropID == requiredPropID) {
                    Debug.Log("We are required prop ID.");
                    // Get the attached prop PV.
                    PhotonView enteringPlayerPV = other.gameObject.GetPhotonView();
                    // Get the rootPlayerID
                    int attemptingPlayerID = other.transform.root.gameObject.GetPhotonView().ViewID;
                    // Are we local and are we who we say we are?
                    if (enteringPlayerPV != null && enteringPlayerPV.IsMine && enteringPlayerPV.Owner.IsLocal) {
                        // Add entering player to insideRoom list.
                        listOfPlayerObjectsInThisRoom.Add(other.gameObject);
                        // Send a message to oManager to start this objective for us.
                        oManager.TryStartRoomObjective(objectiveNum, attemptingPlayerID, initialPropID);
                    }
                } else {
                    Debug.Log("We are NOT required prop ID.");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (oManager != null) {
            // Making sure we that the collision happened with a player "prop".
            if (other.gameObject.tag == "AttachedProp" && other.gameObject.transform.parent != null) {
                PhotonView exitingPlayerPV = other.gameObject.GetPhotonView();
                //Get the rootPlayerID
                int attemptingPlayerID = other.transform.root.gameObject.GetPhotonView().ViewID;
                // Are we local and are we who we say we are?
                if (exitingPlayerPV != null && exitingPlayerPV.IsMine && exitingPlayerPV.Owner.IsLocal) {
                    // Remove leaving player from insideRoom list.
                    listOfPlayerObjectsInThisRoom.Remove(other.gameObject);
                    // Send a message to our oManager to cancel the objective for us.
                    oManager.TryCancelRoomObjective(objectiveNum, attemptingPlayerID);
                }
            }
        }
    }



}
