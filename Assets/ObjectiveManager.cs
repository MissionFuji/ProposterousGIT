using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
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
    private GameplayController gController;
    private PhotonView oMgrPV;
    [SerializeField]
    private List<int> GivenObjectiveNumber = new List<int>();
    private int roomCountDownRemaining;
    private int roomObjectiveTryingToComplete;
    private int completingPlayerID;
    private int numberOfCompletedObjectives;




    private void Awake() {

        // Controller References
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();
        oMgrPV = GetComponent<PhotonView>();

        // If we're the MasterClient, let's generate our list.
        if (PhotonNetwork.IsMasterClient) {
            MasterClientGeneratesNewObjectiveList();
        }

        // Let's force everyone to get a reference of this oManager in GameplayController.
        gController.GetObjectiveManagerReference();


        roomObjectiveTryingToComplete = -1; // Default val.
        completingPlayerID = -1; // Default val.
        numberOfCompletedObjectives = 0; // Default val.

    }

    //This only runs on players who are "props". This shouldn't run for seekers.
    //Initializes ObjectiveManager and asks MasterClient for the objective list.
    public void InitiateObjectiveManager(int localPlayerID) {
            PhotonView lpPV = PhotonView.Find(localPlayerID);
            // If we're not the MC.
            if (!lpPV.Owner.IsMasterClient) {
                // If we own the PV we're going to run RPC's for.
                if (lpPV != null && lpPV.IsMine) {
                // We'll request a list from our MC.
                Debug.Log("We're requestion a list from our MC after we got out map spawned in.");
                oMgrPV.RPC("RPC_RequestObjectiveListFromMaster", RpcTarget.MasterClient, lpPV.ViewID);
                } else {
                    Debug.LogError("Player that is trying to initiate ObjectiveManager has null PV or I don't own the PV?");
                }
            } else {
                //If we're the MC, we already created the list when the map was spawned in. So instead of requestion access, we'll just display the list locally.
                DisplayObjectiveList();
            }
    }

    private void MasterClientGeneratesNewObjectiveList() {
        if (sController != null) {
            int loopCounter = 0;
            while (GivenObjectiveNumber.Count < NumberOfObjectives) { // If we havent finished getting all of our objectives yet 
                int r = Random.Range(0, ObjectiveList_ReadOnly.Count - 1);
                if (!GivenObjectiveNumber.Contains(r)) {
                    GivenObjectiveNumber.Add(r);
                    loopCounter++;
                    Debug.Log("MasterClient is building obj list.");
                }
            }
        }
    }

    private void DisplayObjectiveList() {
        if (sController != null) {
            int loopCounter = 0;
            foreach (int objNum in GivenObjectiveNumber) {
                sController.PopulateObjectiveList(ObjectiveList_ReadOnly[objNum], loopCounter); // We tell our sController what to show, and what line to show it.
                loopCounter++;

                if (loopCounter == GivenObjectiveNumber.Count) {
                    Debug.Log("Displayed all objectives.");
                }

            }
        }
    }

    // Runs on the MasterClient. MC gives prop players the objective list.
    [PunRPC]
    private void RPC_RequestObjectiveListFromMaster(int requestingPlyID) {
        PhotonView targetPlayer = PhotonView.Find(requestingPlyID);

        int[] compressedObjectivesList = GivenObjectiveNumber.ToArray();
        Debug.Log("Sending compressed objective list array to requesting player.");

        oMgrPV.RPC("RPC_GivePlayerAccessToObjectiveList", targetPlayer.Owner, compressedObjectivesList);
    }

    // Runs on all clients who were able to request an objective list.
    [PunRPC]
    private void RPC_GivePlayerAccessToObjectiveList(int[] compressedListOfObjectives) {

        GivenObjectiveNumber = compressedListOfObjectives.ToList<int>();
        DisplayObjectiveList();
    }



    //----------------<Room Objectives>----------------\\

    // Start Room Objective Countdown.
    public void TryStartRoomObjective(int objectiveNum, int attemptingPlayerID) {
        PhotonView attemptPly = PhotonView.Find(attemptingPlayerID);
        if (attemptPly != null && attemptPly.IsMine) { // Let's make sure we are who we say we are.

            // Let's try to clear any current/on-going objectives.
            if (IsInvoking("StartRoomObjectiveCountdown") || completingPlayerID != -1 || roomObjectiveTryingToComplete != -1) {
                TryCancelRoomObjective(-1, attemptingPlayerID);
            }

            // Let's try to start a new objective
            if (GivenObjectiveNumber.Contains(objectiveNum)) {
                // Reference our attempting player locally on this script.
                completingPlayerID = attemptingPlayerID;
                // Reset the countdown to start again.
                roomCountDownRemaining = RoomCountdownTime;
                // We tell our oManager what objective we're working on completing.
                roomObjectiveTryingToComplete = objectiveNum;
                // Start the countdown, kronk.
                InvokeRepeating("StartRoomObjectiveCountdown", 0.1f, 1f);
                Debug.Log("We DO have that objective. Starting it if possible.");
            } else {
                Debug.Log("We don't have that objective.");
            }
        }
    }

    // Cancel Room Objective Countdown.
    public void TryCancelRoomObjective(int objectiveNum, int attemptingPlayerID) {
        PhotonView attemptPly = PhotonView.Find(attemptingPlayerID);
        if (attemptPly != null && attemptPly.IsMine) { // Let's make sure we are who we say we are.
                // Clearing our completingPlayerID var because we canceled the objective.
                completingPlayerID = -1;
                // Cancel the timer.
                CancelInvoke("StartRoomObjectiveCountdown");
                // Reset the countdown time.
                roomCountDownRemaining = RoomCountdownTime;
                // Set attempting complete objective Num to -1 (Because 0 is technically always in our list.)
                roomObjectiveTryingToComplete = -1;
        }
    }

    // Countdown has ended, trying to complete the objective.
    private void TryCompleteRoomObjective(int objectiveToComplete, int completingPlyID) {
        PhotonView cmpltingPly = PhotonView.Find(completingPlyID);
        if (cmpltingPly != null && cmpltingPly.IsMine) { // Let's make sure we are who we say we are.
            if (GivenObjectiveNumber.Contains(roomObjectiveTryingToComplete)) {
                // Let's tell all client that this task is completed by us.
                oMgrPV.RPC("RPC_CompleteRoomObjective", RpcTarget.AllBuffered, objectiveToComplete, completingPlyID);
            } else {
                Debug.Log("Tried to complete objective that was not part of your objective list? Could have been completed by somebody else before you.");
            }
        }
    }

    // The countdown timer for the room objectives.
    private void StartRoomObjectiveCountdown() {
        //Countdown each second.
        roomCountDownRemaining--;
        // If we're done counting down:
        if (roomCountDownRemaining == 0) {
            // Let's try to complete the objective.
            TryCompleteRoomObjective(roomObjectiveTryingToComplete, completingPlayerID);
            // Stop the timer.
            CancelInvoke("StartRoomObjectiveCountdown");
        }
    }

    // Runs on all clients when a player completes an objective.
    [PunRPC]
    private void RPC_CompleteRoomObjective(int objectiveNumber, int completedPlayer) {
        // Visually complete the objective on the UI Locally.
        sController.VisualCompleteObjective(objectiveNumber);
        // Set the objNumber in the list to -1. (We can't just remove an index, so we have to modify it instead.)
        GivenObjectiveNumber[objectiveNumber] = -1;
        // Track # of completed tasks locally.
        numberOfCompletedObjectives++;

        // If we're the MC.
        if (PhotonNetwork.IsMasterClient) {
            // Let's see if we're now done with all of our tasks
            if (numberOfCompletedObjectives == NumberOfObjectives) {
                gController.UpdateGameplayState(4);
            }
        }

        Debug.LogError("OBJECTIVE COMPLETE: " + objectiveNumber.ToString() + ". Finished by player with viewID: " + completedPlayer + ".");
    }


    //----------------</Room Objectives>----------------\\


}
