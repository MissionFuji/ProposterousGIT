using UnityEngine;

public class GameplayController : MonoBehaviour
{
    /*

    Loading Screen Options via RunLoadingScreen(). 1 = company logo, 2 = game logo, 3 = example loading screen?

    Workflow:
    Solid Background.
    Proposterous Logo w/ some kind of support call to action.
    Fades to black
    Company Logo w/ social links
    */



    [SerializeField]
    private int gameplayStatus = -1; // -1 is initial load, 0 is Main Menu, 1 is Pre-Game Room, 2 is In-Game Prep-Phase, 3 is In-Game Active, 4 is In-Game End-Of-Round;



    private ScreenController sController;



    private void Awake() {
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
    }


}
