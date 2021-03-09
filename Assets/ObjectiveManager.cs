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


    // Regular Private Vars
    private ScreenController sController;

    [SerializeField]
    private List<string> GivenObjectivesToDisplay = new List<string>();
    [SerializeField]
    private List<int> GivenObjectiveNumber = new List<int>();



    private void Awake() {
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
    }

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

}
