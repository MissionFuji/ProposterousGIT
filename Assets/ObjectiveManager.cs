using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour {

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
    private List<int> GivenObjectiveNumber = new List<int>();
    private int roomCountDownRemaining;
    private int roomObjectiveTryingToComplete;
    private int completingPlayerID;
    private int numberOfCompletedObjectives;
    private bool canCompleteObjectives = false;
    private int initPropID = -1;
    private GameObject localPlayerRoot;




    private void Awake() {

        // Controller References
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>();
        oMgrPV = GetComponent<PhotonView>();
        localPlayerRoot = (GameObject)PhotonNetwork.LocalPlayer.TagObject;

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
            // If we ARE the mc, we don't need to get the list because we made it in MasterClientGeneratesNewObjectiveList().
        }
    }

    private void MasterClientGeneratesNewObjectiveList() {
        if (sController != null) {
            int loopCounter = 0;
            while (GivenObjectiveNumber.Count < NumberOfObjectives) { // If we havent finished getting all of our objectives yet 
                // Grab a random index of the ReadOnly copy of our objective list.
                int r = Random.Range(0, ObjectiveList_ReadOnly.Count - 1);
                // Make sure our list doesn't already have this objective.
                if (!GivenObjectiveNumber.Contains(r)) {
                    // Add it to our list.
                    GivenObjectiveNumber.Add(r);
                    // Count each iteration.
                    loopCounter++;
                    // Are we done adding objectives?
                    if (loopCounter == NumberOfObjectives) {
                        Debug.Log("MasterClient finished building Objective List.");
                    }
                }
            }
        }
    }

    // Runs on all clients. Currently running from gController where we destroy the "Seeker Door"
    public void DisplayObjectiveList() {

        if (GivenObjectiveNumber.Count > 0) { // We do this to make sure nobody who wasn't approved to get the list can cheese the system and see it early.
            if (sController != null) {
                int loopCounter = 0;

                foreach (int objNum in GivenObjectiveNumber) {

                    // Tell the sController to display our objective.
                    sController.PopulateObjectiveList(ObjectiveList_ReadOnly[objNum], loopCounter); // We tell our sController what to show, and what line to show it.

                    // Counting
                    loopCounter++;

                    // Are we done adding objectives?
                    if (loopCounter == GivenObjectiveNumber.Count) {
                        Debug.Log("Displayed all objectives. Can now complete objectives.");
                        canCompleteObjectives = true;
                    }

                }
            }
        }
    }

    // Runs on the MasterClient. MC gives prop players the objective list.
    [PunRPC]
    private void RPC_RequestObjectiveListFromMaster(int requestingPlyID) {
        // Get requesting player Reference
        PhotonView targetPlayer = PhotonView.Find(requestingPlyID);
        // De-compress list for use.
        int[] compressedObjectivesList = GivenObjectiveNumber.ToArray();
        // Give requesting player access to obj list.
        oMgrPV.RPC("RPC_GivePlayerAccessToObjectiveList", targetPlayer.Owner, compressedObjectivesList);
    }

    // Runs on all clients who were able to request an objective list.
    [PunRPC]
    private void RPC_GivePlayerAccessToObjectiveList(int[] compressedListOfObjectives) {

        // Update Obj List on the back-end.
        GivenObjectiveNumber = compressedListOfObjectives.ToList<int>();
        // We run DisplayObjectiveList through gController when the "SeekerDoor" get destroyed.
    }



    //----------------<Room Objectives>----------------\\

    // Start Room Objective Countdown.
    public void TryStartRoomObjective(int objectiveNum, int attemptingPlayerID, int initialPropID) {
        if (canCompleteObjectives) {
            PhotonView attemptPly = PhotonView.Find(attemptingPlayerID);
            if (attemptPly != null && attemptPly.IsMine) { // Let's make sure we are who we say we are.

                // Let's try to clear any current/on-going objectives.
                if (IsInvoking("StartRoomObjectiveCountdown") || completingPlayerID != -1 || roomObjectiveTryingToComplete != -1) {
                    TryCancelRoomObjective(-1, attemptingPlayerID);
                }

                // Let's try to start a new objective
                if (GivenObjectiveNumber.Contains(objectiveNum)) {
                    // Save our original propID when we started objective. Just to make sure it doesn't change.
                    initPropID = initialPropID;
                    // Reference our attempting player locally on this script.
                    completingPlayerID = attemptingPlayerID;
                    // Reset the countdown to start again.
                    roomCountDownRemaining = RoomCountdownTime;
                    // We tell our oManager what objective we're working on completing.
                    roomObjectiveTryingToComplete = objectiveNum;
                    // Start the countdown, kronk.
                    InvokeRepeating("StartRoomObjectiveCountdown", 0.1f, 1f);
                } 
            }
        }
    }

    // Cancel Room Objective Countdown.
    public void TryCancelRoomObjective(int objectiveNum, int attemptingPlayerID) {
        if (canCompleteObjectives) {
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
    }

    // Countdown has ended, trying to complete the objective.
    private void TryCompleteRoomObjective(int objectiveToComplete, int completingPlyID) {
        if (canCompleteObjectives) {
            PhotonView cmpltingPly = PhotonView.Find(completingPlyID);
            if (cmpltingPly != null && cmpltingPly.IsMine) { // Let's make sure we are who we say we are.
                if (GivenObjectiveNumber.Contains(roomObjectiveTryingToComplete)) {

                    int lineNum = GivenObjectiveNumber.IndexOf(roomObjectiveTryingToComplete);
                    // Let's tell all client that this task is completed by us.
                    oMgrPV.RPC("RPC_CompleteRoomObjective", RpcTarget.AllBuffered, objectiveToComplete, lineNum, completingPlyID);
                } else {
                    Debug.Log("Tried to complete objective that was not part of your objective list? Could have been completed by somebody else before you.");
                }
            }
        }
    }

    // The countdown timer for the room objectives.
    private void StartRoomObjectiveCountdown() {

        // Is our player reference null?
        if (localPlayerRoot != null) {
            // Unfortunately, we have to keep re-grabbing this reference because if a player DOES change, this will come back null.
            PropInteraction rootPlayerPropPI = localPlayerRoot.transform.Find("PropHolder").GetChild(0).gameObject.GetComponent<PropInteraction>();
            // Make sure PropInteration of the prop isn't null.
            if (rootPlayerPropPI != null) {
                // The initial PropID matches the current PropID.
                if (rootPlayerPropPI.GetPropID() == initPropID) {
                    //Countdown each second.
                    roomCountDownRemaining--;
                    // If we're done counting down:
                    if (roomCountDownRemaining == 0) {
                        // Let's try to complete the objective.
                        TryCompleteRoomObjective(roomObjectiveTryingToComplete, completingPlayerID);
                        // Stop the timer.
                        CancelInvoke("StartRoomObjectiveCountdown");
                    }
                } else {
                    Debug.Log("Looks like you've changed props. Cancelling your current objective.");
                    // Try to cancel the objective because the player changed props.
                    TryCancelRoomObjective(roomObjectiveTryingToComplete, completingPlayerID);
                    // Stop the timer.
                    CancelInvoke("StartRoomObjectiveCountdown");
                }
            }
        }
    }

    // Runs on all clients when a player completes an objective.
    [PunRPC]
    private void RPC_CompleteRoomObjective(int objectiveNumber, int lineNumber, int completedPlayer) {

        // Only our prop players will have a populated list. Let's make sure we're a prop if we're modifying this value.
        if (GivenObjectiveNumber.Count > 0) {
            // Set the objNumber in the list to -1. (We can't just remove an index, so we have to modify it instead.)
            GivenObjectiveNumber[lineNumber] = -1;
        }

        // Visually complete the objective on the UI Locally.
        sController.VisualCompleteObjective(lineNumber);

        // Track # of completed tasks locally.
        numberOfCompletedObjectives++;

        // If we're the MC.
        if (PhotonNetwork.IsMasterClient) {
            // Let's see if we're now done with all of our tasks
            if (numberOfCompletedObjectives == NumberOfObjectives) {
                gController.UpdateGameplayState(4);
                sController.ClearObjectiveList();
                GivenObjectiveNumber.Clear();
            }
        }

        Debug.Log("OBJECTIVE COMPLETE: " + objectiveNumber.ToString() + ". Finished by player with viewID: " + completedPlayer + ".");
    }


    //----------------</Room Objectives>----------------\\


}
