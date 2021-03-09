using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine.UI;

public class ObjectiveManager : MonoBehaviour
{

    [Header("Objective Manager Settings")]

    [Tooltip("Taunting, Proximity Chat, Talking Over Seeker Radio, etc.")]
    [Range(0, 6)]
    [SerializeField]
    private int SocialObjectives;
    [Tooltip("Possess X object in x room for x seconds, etc.")]
    [Range(0, 6)]
    [SerializeField]
    private int PossessObjectives;
    [Tooltip("Turn off the lights, turn off X object, etc.")]
    [Range(0, 6)]
    [SerializeField]
    private int InteractObjectives;

    [SerializeField]
    private List<string> ListOfRooms = new List<string>(); // This list is created via inspector and DOESN'T get modified otherwise.

    [SerializeField]
    private List<string> ListOfProps = new List<string>(); // This list is created via inspector and DOESN'T get modified otherwise.

    [SerializeField]
    private List<string> DisplayTheseObjectives = new List<string>(); // This list will be populated with the target player's randomly selected objectives.

    [SerializeField]
    private int[] GivenObjectives;

    // Regular Private Vars
    private ScreenController sController;

    //figure out what kind of task to give. Haunt, Interact, Possess for X Seconds in X room, Taunt in X room
    // 

    private void Awake() {
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
    }

    public void InitiateObjectiveManager(int localPlayerID) {
        PhotonView lpPV = PhotonView.Find(localPlayerID);
        if (lpPV != null && lpPV.IsMine) { // Let's make sure we are who we say we are.
            if (sController != null) {
                sController.PopulateObjectiveList(DisplayTheseObjectives);
            }
        } else {
            Debug.LogError("Player that is trying to initiate ObjectiveManager has null PV or I don't own the PV?");
        }
    }

}
