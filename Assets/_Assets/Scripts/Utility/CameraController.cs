using UnityEngine;

public class CameraController : MonoBehaviour
{

    public float mouseSensitivity = 10;
    public Transform target;
    public float dstFromTarget;
    public Vector2 pitchMinMax = new Vector2(-40, 85);
    public float rotationSmoothTime = .12f;
    public Vector3 CameraPositionWhenNotInGameFailsafe = new Vector3(0f, 10.5f, -53f);
    public float scrollSpeed;
    public float minDist, maxDist;

    private bool playerReady = false;
    private bool lockCursor;
    private ScreenController sController;
    private Vector3 rotationSmoothVelocity;
    private Vector3 currentRotation;

    float yaw;
    float pitch;

    //Camera pos above head dynamic vars
    [SerializeField]
    private LayerMask layerToHit;
    [SerializeField]
    private Vector3 propYOffset;
    [SerializeField]
    private float camLerpSpeed;
    [SerializeField]
    private Vector3 mostRecentHit;
    [SerializeField]
    private Vector3 yFollowOffset;
    private Vector3 moveSmoothVel;

    public void ReadyCamera(Transform tar, bool result) {
        if (result) {
            target = tar;
            playerReady = true;
        } else {
            playerReady = false;
            target = null;
        }
    }

    private void Awake() {
        sController = GameObject.FindGameObjectWithTag("ScreenController").GetComponent<ScreenController>();
        mostRecentHit = gameObject.transform.position;
    }

    void Start() {
        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update() {
        float wheelVal = -Input.mouseScrollDelta.y;
        if (wheelVal != 0) { // scrolling
            dstFromTarget =  Mathf.Clamp(dstFromTarget += wheelVal * scrollSpeed, minDist, maxDist);
        }

    }

    void LateUpdate() {
        if (playerReady) { // If we have an active player.
            if (sController.ActiveMenuOnScreen == null) { // We do this check so we don't rotate the camera around out player while a menu is open.
                
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
                currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
                transform.eulerAngles = currentRotation;

                RaycastHit hit;
                Vector3 originPoint = new Vector3(target.position.x, target.position.y + 100f, target.position.z);
                // Are we hitting default layer? (LocalPlayer will always be on default layer.) Is our tag also LocalPlayer?
                if (Physics.Raycast(originPoint, -Vector3.up, out hit, 200f, layerToHit) && (hit.collider.gameObject.transform.root.tag == "LocalPlayer")) {
                    Vector3 smoothMovesStud = Vector3.SmoothDamp(transform.position, (hit.point + yFollowOffset) - transform.forward * dstFromTarget , ref moveSmoothVel, camLerpSpeed);
                    transform.position = smoothMovesStud;
                    Debug.Log("yes");
                } else {
                    Debug.Log("No?");
                }


            }
        } else { // If our player isn't active/on-screen.
                if (Vector3.Distance(transform.position, CameraPositionWhenNotInGameFailsafe) > 1f) {
                    transform.position = Vector3.Lerp(transform.position, CameraPositionWhenNotInGameFailsafe, Time.deltaTime * 1f); // A backup position.
                Debug.Log("Moving to backup pos.");
                }
                if (!Mathf.Approximately(Mathf.Abs(Quaternion.Dot(transform.rotation, Quaternion.identity)), 2.0f)) {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * 1f);
                }
        }
    }



}
