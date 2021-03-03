using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenController : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    //Major references
    #region
    private PhotonView pv;
    private PlayerPropertiesController ppc;
    private CameraController cController;
    private LobbySystem lobbySys;
    #endregion

    // Escape Menu References + Cursor Reference.
    #region
    private GameObject cursorSprite;
    private GameObject canvas;
    private GameObject MainMenuItems;
    private GameObject RoomMenuItems;
    private GameObject ConfirmExitMenu;
    private GameObject OptionsMenu;
    private Text CountDownTimer;
    private Text GameTimeLeft;
    public GameObject ActiveMenuOnScreen = null;
    #endregion

    //References used for Loading Screen Updates.
    #region

    [SerializeField]
    private List<Sprite> backgroundSpriteList = new List<Sprite>();
    [SerializeField]
    private List<Sprite> hoveringSpriteList = new List<Sprite>();
    [SerializeField]
    private float loadingScreenLerpSpeed;

    private Image targetBackgroundImg;
    private Image targetHoverImg;
    private bool isLoading = false;
    private bool isFadingIn = false;
    private Color opaqueColor = new Color(1f, 1f, 1f, 1f);
    private Color transparentColor = new Color(1f, 1f, 1f, 0f);
    #endregion

    void Awake() {
        pv = gameObject.GetComponent<PhotonView>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
        cController = Camera.main.GetComponent<CameraController>();
        lobbySys = GameObject.FindGameObjectWithTag("LobbySystem").GetComponent<LobbySystem>();
        canvas = GameObject.FindGameObjectWithTag("RootCanvas");
        CountDownTimer = canvas.gameObject.transform.Find("RoomUI/CountDownTimer").gameObject.GetComponent<Text>();
        GameTimeLeft = canvas.gameObject.transform.Find("RoomUI/GameTimeLeft").gameObject.GetComponent<Text>();
        cursorSprite = canvas.gameObject.transform.Find("RoomUI/CursorImage").gameObject;
        targetBackgroundImg = canvas.transform.Find("BlankBackgroundScreen").gameObject.GetComponent<Image>();
        targetHoverImg = targetBackgroundImg.transform.GetChild(0).gameObject.GetComponent<Image>();
        RoomMenuItems = canvas.transform.Find("RoomMenuItems").gameObject;
        MainMenuItems = canvas.transform.Find("MainMenuItems").gameObject;
        ConfirmExitMenu = canvas.transform.Find("ConfirmExitMenu").gameObject;
        OptionsMenu = canvas.transform.Find("OptionsMenu").gameObject;
        Invoke("InitialIntroScreenDelay", 1f);
    }


    // loadingScreenRoutines: 0 gameIntro, 1 loadIntoRoom, 2 loadIntoGame
    public void RunLoadingScreen(int loadingScreenRoutine) {
        if (targetBackgroundImg != null && targetHoverImg != null) { // Make sure we have references to our canvas objects.
            if (loadingScreenRoutine == 0) { // Game intro
                targetBackgroundImg.sprite = backgroundSpriteList[0];
                targetHoverImg.sprite = hoveringSpriteList[0];
            } else if (loadingScreenRoutine == 1) { // Load Into Room
                targetBackgroundImg.sprite = backgroundSpriteList[1];
                targetHoverImg.sprite = hoveringSpriteList[1];
            } else if (loadingScreenRoutine == 2) { // Load Into Game
                targetBackgroundImg.sprite = backgroundSpriteList[2];
                targetHoverImg.sprite = hoveringSpriteList[1];
            } else if (loadingScreenRoutine == 3) {
                targetBackgroundImg.sprite = backgroundSpriteList[1];
                targetHoverImg.sprite = hoveringSpriteList[2];
            } else {
                targetBackgroundImg.sprite = backgroundSpriteList[0];
                targetHoverImg.sprite = hoveringSpriteList[0];
                Debug.LogError("Failsafe Loading Screen Ran. Routine selected was out of range.");
            }
            //These enable the loading screen to run through Update().
            isLoading = true;
            isFadingIn = true;
        }
    }

    //We run this to end our current loading screen after x seconds.
    public void EndLoadingScreen(float timeBeforeEnd) {
        if (isLoading) {
            if (isFadingIn) {
                if (!IsInvoking("Invoke_EndLoadingScreen")) {
                    Invoke("Invoke_EndLoadingScreen", timeBeforeEnd);
                }
            }
        }
    }


    void Update() {

        //Loading Screens
        #region
        if (isLoading) { // are we loading?
            if (isFadingIn) { // are we increasing opacity?
                if (targetHoverImg.color.a >= 0.98f && targetBackgroundImg.color.a >= 0.98f) { // If it's almost fully opaque
                    if (targetHoverImg.color != opaqueColor && targetBackgroundImg.color != opaqueColor) { // Let's set it to full opaque.
                        targetBackgroundImg.color = opaqueColor;
                        targetHoverImg.color = opaqueColor;
                    }
                } else { // Let's lerp color towards opaque
                    targetBackgroundImg.color = Color.Lerp(targetBackgroundImg.color, opaqueColor, Time.deltaTime * loadingScreenLerpSpeed);
                    targetHoverImg.color = Color.Lerp(targetHoverImg.color, opaqueColor, Time.deltaTime * loadingScreenLerpSpeed);
                }
            } else { // are we decreasing opacity?
                if (targetHoverImg.color.a <= 0.02f) { // Are we close to transparent?
                    if (targetHoverImg.color != transparentColor && targetBackgroundImg.color != transparentColor) { // Since we're close, let's just manually set it.
                        targetHoverImg.color = transparentColor;
                        targetBackgroundImg.color = transparentColor;
                        isLoading = false; // We do this to break the loop. RunLoadingScreen must be run to restart this process.
                    }
                } else {
                    targetHoverImg.color = Color.Lerp(targetHoverImg.color, transparentColor, Time.deltaTime * loadingScreenLerpSpeed);
                    targetBackgroundImg.color = Color.Lerp(targetBackgroundImg.color, transparentColor, Time.deltaTime * loadingScreenLerpSpeed);
                }
            }
        }
        #endregion

        //Escape Menu/Menu Navigation.
        #region

        if (Input.GetKeyDown(KeyCode.Escape)) {
            Debug.Log("Escape Pressed.");
            if (isLoading == false) { // We only want to allow use of the menu items if there is no loading screen.
                if (ActiveMenuOnScreen == null) {
                    if (!PhotonNetwork.InRoom) { // Not in a room.

                    } else { //We're in a room
                             //Trying to Escape while in a game. Showing RoomMenuItems.
                        RoomMenuItems.SetActive(true);
                        ActiveMenuOnScreen = RoomMenuItems;
                    }
                } else {
                    //There's already a screen up, is it the confirm exit screen?
                    if (ActiveMenuOnScreen == ConfirmExitMenu) {
                        MainMenuItems.SetActive(true);
                    }
                    // Let's close any open screen.
                    ActiveMenuOnScreen.SetActive(false);
                    ActiveMenuOnScreen = null;
                }
            }
        }

        //Movestate and Cursor Control.
        if (ActiveMenuOnScreen != null) {
            if (cursorSprite.activeSelf == true) {
                cursorSprite.SetActive(false); // disables cursor object.
            }
            if (ppc != null && ppc.playerIsFrozen) {
                ppc.playerIsFrozen = true; // freezes our player.
            }
        } else {
            if (cursorSprite.activeSelf == false) {
                cursorSprite.SetActive(true); // enables cursor object.
            }
            if (ppc != null && ppc.playerIsFrozen) {
                ppc.playerIsFrozen = false; // unfreezes our player.
            }
        }
        #endregion
    }



    //UI Updates/Escape Menu UI.
    #region



    public void OnClick_Resume() {
        if (ActiveMenuOnScreen != null) {
            if (ActiveMenuOnScreen == RoomMenuItems) {
                RoomMenuItems.SetActive(false);
                ActiveMenuOnScreen = null;
            }
        }
    }

    public void OnClick_Shop() {
        Application.OpenURL("http://unity3d.com/");
    }

    public void OnClick_Options() {
        if (ActiveMenuOnScreen != null) {
            ActiveMenuOnScreen.SetActive(false);
            ActiveMenuOnScreen = null;
        }
        if (MainMenuItems.activeSelf == true) {
            MainMenuItems.SetActive(false);
        }
        OptionsMenu.SetActive(true);
        ActiveMenuOnScreen = OptionsMenu;
    }

    public void OnClick_LeaveGame() {
        if (ActiveMenuOnScreen != null) {
            ActiveMenuOnScreen.SetActive(false);
            ConfirmExitMenu.SetActive(true);
            ActiveMenuOnScreen = ConfirmExitMenu;
        }
    }

    public void OnClick_Back() {
        if (ActiveMenuOnScreen != null) {
            if (ActiveMenuOnScreen == OptionsMenu) {
                OptionsMenu.SetActive(false);
                RoomMenuItems.SetActive(true);
                ActiveMenuOnScreen = RoomMenuItems;
            } else if (ActiveMenuOnScreen == RoomMenuItems) {
                RoomMenuItems.SetActive(false);
                ActiveMenuOnScreen = null;
            } else if (ActiveMenuOnScreen == ConfirmExitMenu) {
                ConfirmExitMenu.SetActive(false);
                RoomMenuItems.SetActive(true);
                ActiveMenuOnScreen = RoomMenuItems;
            } else {
                Debug.LogError("Issue with UI in game controller. There's an unknown ActiveMenuOnScreen..");
            }
        }
    }

    public void OnClick_ConfirmLeave() {
        RunLoadingScreen(2);
        Invoke("Invoke_OnClick_ConfirmLeave", 0.5f);
    }
    #endregion


    private void Invoke_EndLoadingScreen() {
        isFadingIn = false;
    }


    private void Invoke_OnClick_ConfirmLeave() {
        if (ActiveMenuOnScreen != null) {
            ActiveMenuOnScreen.SetActive(false);
            ActiveMenuOnScreen = null;
        }
        cController.ReadyCamera(transform, false); // Before we leave the room, we make sure our camera controller knows.
        if (pv.IsMine) {
            ppc.HostDisconnecting(pv.ViewID);
        } else {
            ppc.ClientDisconnecting(pv.ViewID);
        }
    }

    private void InitialIntroScreenDelay() {
        RunLoadingScreen(0);
        lobbySys.SetupPhotonNetwork();
    }

    public void UpdateCountDown(int currentCount) {
        if (currentCount > 0) {
            CountDownTimer.text = currentCount.ToString();
        } else if (currentCount == 0) {
            CountDownTimer.text = "Go!";
            Invoke("Invoke_ClearTimerText", 1f); //Clear timer text after 1 second.
        }
    }

    public void UpdateGameTimeLeft(int currentCount) {
        if (currentCount > 0) {
            GameTimeLeft.text = currentCount.ToString();
        } else if (currentCount == 0) {
            GameTimeLeft.text = "";
        }
    }

    private void Invoke_ClearTimerText() {
        if (CountDownTimer.gameObject.activeSelf == false) {
            CountDownTimer.gameObject.SetActive(true);
        }
        CountDownTimer.text = "";
    }



}
