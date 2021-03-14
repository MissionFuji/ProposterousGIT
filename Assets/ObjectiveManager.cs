using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine.UI;

public class ObjectiveManager : MonoBehaviour
{

    [Header("Objective Manager Settings")]
    [Tooltip("The number of objectives per prop player.")]
    [Range(0, 6)]
    [SerializeField]
    private int NumberOfObjectives;

    [SerializeField]
    private List<string> ObjectiveList_ReadOnly = new List<string>(); // This list will be populated with the target player's randomly selected objectives.

    [SerializeField]
    private int RoomCountdownTime;


    // Regular Private Vars
    private ScreenController sController;
    private List<string> GivenObjectivesToDisplay = new List<string>();
    private List<int> GivenObjectiveNumber = new List<int>();
    private int roomCountDownRemaining;
    private int roomObjectiveTryingToComplete;




    private void Awake() {
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        roomObjectiveTryingToComplete = -1;
    }

    //Initializes ObjectiveManager and Builds List of Objectives for players who need them.
    public void InitiateObjectiveManager(int localPlayerID) {
        PhotonView lpPV = PhotonView.Find(localPlayerID);
        if (lpPV != null && lpPV.IsMine) { // Let's make sure we are who we say we are.
            if (sController != null) {
                int loopCounter = 0;
                while (GivenObjectivesToDisplay.Count < NumberOfObjectives) { // If we havent finished getting all of our objectives yet 
                    int r = Random.Range(0, ObjectiveList_ReadOnly.Count - 1);
                    if (!GivenObjectivesToDisplay.Contains(ObjectiveList_ReadOnly[r])) {
                        sController.PopulateObjectiveList(ObjectiveList_ReadOnly[r], loopCounter); // We tell our sController what to show, and what line to show it.
                        GivenObjectivesToDisplay.Add(ObjectiveList_ReadOnly[r]);
                        GivenObjectiveNumber.Add(r);
                        loopCounter++;
                    }
                }
            }
        } else {
            Debug.LogError("Player that is trying to initiate ObjectiveManager has null PV or I don't own the PV?");
        }
    }

    //----------------<Room Objectives>----------------\\

    // Start Room Objective Countdown.
    public void TryStartRoomObjective(int objectiveNum) {
        if (GivenObjectiveNumber.Contains(objectiveNum)) {
            roomCountDownRemaining = RoomCountdownTime;
            roomObjectiveTryingToComplete = objectiveNum;
            InvokeRepeating("StartRoomObjectiveCountdown", 0.1f, 1f);
            Debug.Log("We DO have that objective. Starting it if possible.");
        } else {
            Debug.Log("We don't have that objective.");
        }
    }

    // Cancel Room Objective Countdown.
    public void TryCancelRoomObjective(int objectiveNum) {
        if (GivenObjectiveNumber.Contains(objectiveNum)) {
            // Cancel the timer.
            CancelInvoke("StartRoomObjectiveCountdown");
            // Reset the countdown time.
            roomCountDownRemaining = RoomCountdownTime;
            // Set attempting complete objective Num to -1 (Because 0 is technically always in our list.)
            roomObjectiveTryingToComplete = -1;
            Debug.Log("We DO have that objective. Cancelling.");
        } else {
            Debug.Log("We don't have that objective. Can't Cancel.");
        }
    }

    // Countdown has ended, trying to complete the objective.
    private void TryCompleteRoomObjective(int objectiveToComplete) {
        if (GivenObjectiveNumber.Contains(roomObjectiveTryingToComplete)) {
            GivenObjectiveNumber.Remove(objectiveToComplete);
            Debug.LogError("OBJECTIVE COMPLETE!");
        } else {
            Debug.Log("Tried to complete objective that was not part of your objective list?.. FAILED.");
        }
    }

    // The countdown timer for the room objectives.
    private void StartRoomObjectiveCountdown() {
        roomCountDownRemaining--;
        if (roomCountDownRemaining == 0) {
            //Let's try to complete the objective.
            TryCompleteRoomObjective(roomObjectiveTryingToComplete);
            CancelInvoke("StartRoomObjectiveCountdown");
            Debug.Log("TryComplete countdown over, tying to finish objective.");
        } else {
            Debug.Log("Pending Time Until Room Objective Complete: " + roomCountDownRemaining.ToString());
        }
    }

    //----------------</Room Objectives>----------------\\


}
