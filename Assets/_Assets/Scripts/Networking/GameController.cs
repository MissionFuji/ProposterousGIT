using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    //-1 default, 0 main menu(connected to master), 1 in a room before map is selected, 2 in-game prep phase,
    //3 active on-going game, 4 end of game sequence, 5 forced loading screen.
    [SerializeField]
    private int GameState = -1;
    private PhotonView pv;
    private PlayerPropertiesController ppc;
    private int moveStateBeforePaused = -1;

    private GameObject canvas;
    private GameObject cursorSprite;
    private GameObject MainMenuBG;
    private GameObject MainMenuItems;
    private Image MainMenuBGImg;
    private Color BGImgOpaque, BGImgTransparent;
    private GameObject RoomMenuItems;
    private GameObject ConfirmExitMenu;
    private GameObject OptionsMenu;
    private GameObject ActiveMenuOnScreen = null;


    void Awake() {
        pv = gameObject.GetComponent<PhotonView>();
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
        canvas = GameObject.FindGameObjectWithTag("RootCanvas");
        cursorSprite = canvas.gameObject.transform.Find("RoomUI/CursorImage").gameObject;
        MainMenuBG = canvas.transform.Find("MainMenuBG").gameObject;
        MainMenuBGImg = MainMenuBG.GetComponent<Image>();
        BGImgOpaque = new Color(1f,1f,1f,1f);
        BGImgTransparent = new Color(1f, 1f, 1f, 0f);
        RoomMenuItems = canvas.transform.Find("RoomMenuItems").gameObject;
        MainMenuItems = canvas.transform.Find("MainMenuItems").gameObject;
        ConfirmExitMenu = canvas.transform.Find("ConfirmExitMenu").gameObject;
        OptionsMenu = canvas.transform.Find("OptionsMenu").gameObject;
    }


    void Update() {

        if (GameState > 0) {
            if (MainMenuBGImg.color.a > 0.025f) {
                MainMenuBGImg.color = Color.Lerp(MainMenuBGImg.color, BGImgTransparent, Time.deltaTime * 1f);
             } else {
                MainMenuBGImg.color = BGImgTransparent;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            Debug.Log("Escape Pressed.");
            if (moveStateBeforePaused == -1) {
                moveStateBeforePaused = ppc.moveState;
            }
            if (ActiveMenuOnScreen == null) {
                if (GameState == 0) {
                    //Are you sure you want to quit?
                    if (MainMenuItems.activeSelf == true) {
                        MainMenuItems.SetActive(false);
                    }
                    ConfirmExitMenu.SetActive(true);
                    ActiveMenuOnScreen = ConfirmExitMenu;
                } else if (GameState > 0 && GameState < 5) {
                    //RoomMenuItems
                    RoomMenuItems.SetActive(true);
                    ActiveMenuOnScreen = RoomMenuItems;
                }
            } else {
                if (ActiveMenuOnScreen == ConfirmExitMenu) {
                    MainMenuItems.SetActive(true);
                }
                ActiveMenuOnScreen.SetActive(false);
                ActiveMenuOnScreen = null;
            }
        }

        if (ActiveMenuOnScreen != null) {
            if (cursorSprite.activeSelf == true) {
                cursorSprite.SetActive(false);
            }
            if (ppc != null && ppc.moveState != 0) {
                ppc.moveState = 0;
            }
        } else {
            if (cursorSprite.activeSelf == false) {
                cursorSprite.SetActive(true);
            }
            if (ppc != null && ppc.moveState == 0) {
                ppc.moveState = moveStateBeforePaused;
                moveStateBeforePaused = -1;
            }
        }

    }

    public void UpdateGameState(int gameStateInt) {
        GameState = gameStateInt;
        Debug.Log("GAME STATE UPDATED - " + gameStateInt.ToString());
        if (gameStateInt == 0) {
            MainMenuBGImg.color = BGImgOpaque; // back in the main menu, so let's put that BG back up there.
        }
    }

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
}
