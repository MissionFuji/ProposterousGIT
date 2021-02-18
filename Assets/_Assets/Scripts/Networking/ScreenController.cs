using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenController : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    //-1 initial, 0 main menu(connected to master), 1 in a room before map is selected, 2 in-game prep phase,
    //3 active on-going game, 4 end of game sequence, 5 forced loading screen.
    private int ScreenState = -1;

    //Major references
    #region
    private PhotonView pv;
    private PlayerPropertiesController ppc;
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
    private GameObject ActiveMenuOnScreen = null;
    #endregion

    #region
    //References used for Loading Screen Updates.

    [SerializeField] // 0 black, 1 red
    private List<Sprite> backgroundSpriteList = new List<Sprite>();
    [SerializeField] // 0 gameintro, 1 companyinfo
    private List<Sprite> hoveringSpriteList = new List<Sprite>();
    [SerializeField]
    private float loadingScreenLerpSpeed;

    private Image targetBackgroundImg;
    private Image targetHoverImg;
    [SerializeField]
    private bool isLoading = false;
    [SerializeField]
    private bool firstLoadGameLogoReady = false;
    [SerializeField]
    private bool firstLoadCompanyLogoReady = false;
    [SerializeField]
    private bool isFadingIn = false;
    [SerializeField]
    private bool isKeepingOldBackgroundActive = false;
    private Color opaqueColor = new Color(1f, 1f, 1f, 1f);
    private Color transparentColor = new Color(1f, 1f, 1f, 0f);
    #endregion

    void Awake() {
        pv = gameObject.GetComponent<PhotonView>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
        lobbySys = GameObject.FindGameObjectWithTag("LobbySystem").GetComponent<LobbySystem>();
        canvas = GameObject.FindGameObjectWithTag("RootCanvas");
        cursorSprite = canvas.gameObject.transform.Find("RoomUI/CursorImage").gameObject;
        targetBackgroundImg = canvas.transform.Find("BlankBackgroundScreen").gameObject.GetComponent<Image>();
        targetHoverImg = targetBackgroundImg.transform.GetChild(0).gameObject.GetComponent<Image>();
        RoomMenuItems = canvas.transform.Find("RoomMenuItems").gameObject;
        MainMenuItems = canvas.transform.Find("MainMenuItems").gameObject;
        ConfirmExitMenu = canvas.transform.Find("ConfirmExitMenu").gameObject;
        OptionsMenu = canvas.transform.Find("OptionsMenu").gameObject;
        Invoke("InitialScreenDelay", 2f);
    }


    // loadingScreenRoutines: 0 gameIntro, 1 companyIntro, 2 loadIntoRoom, 3 loadIntoGame.
    public void RunLoadingScreen(int loadingScreenRoutine, bool keepOldBackgroundActive) {
        if (targetBackgroundImg != null && targetHoverImg != null) { // Make sure we have references to our canvas objects.

            isKeepingOldBackgroundActive = keepOldBackgroundActive; // Set local bool depending if we want to keep background there after a loading screen.

            if (loadingScreenRoutine == 0) { // Game intro
                firstLoadGameLogoReady = true;
                targetBackgroundImg.sprite = backgroundSpriteList[0];
                targetHoverImg.sprite = hoveringSpriteList[0];
            } else if (loadingScreenRoutine == 1) { // Company Intro
                firstLoadCompanyLogoReady = true;
                targetBackgroundImg.sprite = backgroundSpriteList[0];
                targetHoverImg.sprite = hoveringSpriteList[1];
            } else if (loadingScreenRoutine == 2) { // Load Into Room
                targetBackgroundImg.sprite = backgroundSpriteList[1];
                targetHoverImg.sprite = hoveringSpriteList[0];
            } else if (loadingScreenRoutine == 3) { // Load Into Game

            }

            isLoading = true;
            isFadingIn = true;

            Debug.Log("Ran RunLoadingScreen.");

        }
    }


    void Update() {

        //Loading Screens
        #region
        if (isLoading) { // are we loading?
            if (isFadingIn) { // are we increasing opacity?
                if ((targetHoverImg.color.a >= 0.975f) && (targetBackgroundImg.color.a >= 0.975f)) {
                    if (!IsInvoking("AfterWaitStartFadeOut")) {
                        if (firstLoadGameLogoReady || firstLoadCompanyLogoReady) {
                            Invoke("AfterWaitStartFadeOut", 1f);
                            Debug.Log("Started 1 sec invoke.");
                        } else {
                            Invoke("AfterWaitStartFadeOut", 5f);
                            Debug.Log("Started 5 sec invoke.");
                        }
                    }
                } else {
                    targetBackgroundImg.color = Color.Lerp(targetBackgroundImg.color, opaqueColor, Time.deltaTime * loadingScreenLerpSpeed);
                    targetHoverImg.color = Color.Lerp(targetHoverImg.color, opaqueColor, Time.deltaTime * loadingScreenLerpSpeed);
                    Debug.Log("Test.");
                }
            } else { // are we decreasing opacity?
                if (!isKeepingOldBackgroundActive) { // We're not keeping old background.
                    if ((targetHoverImg.color.a <= 0.025f) && (targetBackgroundImg.color.a <= 0.025f)) {
                        if (!IsInvoking("AfterWaitStartFadeIn")) {
                            targetBackgroundImg.color = transparentColor;
                            targetHoverImg.color = transparentColor;
                            targetHoverImg.color = transparentColor;
                            Invoke("AfterWaitStartFadeIn", 1f);
                            isLoading = false;
                        }
                    } else {
                        targetBackgroundImg.color = Color.Lerp(targetBackgroundImg.color, transparentColor, Time.deltaTime * loadingScreenLerpSpeed);
                        targetHoverImg.color = Color.Lerp(targetHoverImg.color, transparentColor, Time.deltaTime * loadingScreenLerpSpeed);
                    }
                } else { // We are keeping old background. Don't fade it out.
                    if (targetHoverImg.color.a <= 0.025f) {
                        //This bit only used for initial load screen. (introduction screen)
                        Debug.Log("hoverImg full set to transparent.");
                        if (!IsInvoking("AfterWaitStartFadeIn")) {
                            targetHoverImg.color = transparentColor;
                            Invoke("AfterWaitStartFadeIn", 1f);
                            isLoading = false;
                        }
                    } else {
                        Debug.Log("fading hoverImg to transparent.");
                        targetHoverImg.color = Color.Lerp(targetHoverImg.color, transparentColor, Time.deltaTime * loadingScreenLerpSpeed);
                    }
                }
            }


        }
        #endregion

        //Testing loading screen
        if (Input.GetKeyDown(KeyCode.P)) {
            RunLoadingScreen(2, false);
            Debug.Log("TESTING ON-COMMAND LOADSCREENS.");
        }


            //Escape Menu/Menu Navigation.
            #region

            if (Input.GetKeyDown(KeyCode.Escape)) {
            Debug.Log("Escape Pressed.");

            if (ActiveMenuOnScreen == null) {
                if (ScreenState == 0) {
                    //Trying to Escape at the main menu. No effect, press Exit Game.
                } else if (ScreenState > 0 && ScreenState < 5) {
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
        if (ActiveMenuOnScreen != null) {
            ActiveMenuOnScreen.SetActive(false);
            ActiveMenuOnScreen = null;
        }
        if (pv.IsMine) {
            ppc.HostDisconnecting(pv.ViewID);
        } else {
            ppc.ClientDisconnecting(pv.ViewID);
        }
    }
    #endregion

    private void AfterWaitStartFadeOut() {
        if (isLoading) {
            if (isFadingIn) {
                isFadingIn = false;
                Debug.Log("ready to go trans when needed.");
            }
        }
    }

    private void AfterWaitStartFadeIn() {
        if (!isLoading) {
            if (!isFadingIn) {
                Debug.Log("ready to go opaque when needed.");
                if (firstLoadGameLogoReady) {
                    firstLoadGameLogoReady = false;
                    RunLoadingScreen(1, false);
                } else if (firstLoadCompanyLogoReady) {
                    firstLoadCompanyLogoReady = false;
                    lobbySys.SetupPhotonNetwork();
                }

                isLoading = true;
            }
        }
    }

    private void InitialScreenDelay() {
        RunLoadingScreen(0, true);
    }

    public void UpdateScreenState(int screenStateInt) {
        ScreenState = screenStateInt;
    }
}
