using UnityEngine;

public class CameraController : MonoBehaviour {

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
    [SerializeField]
    private float hitBlockingObjectLerpSpeed;
    [SerializeField]
    private LayerMask layersToIgnorePhysCam;

    [SerializeField]
    private float lerpBetweenSpeed; //Lerp between pre raycast and new one. Trying to stop the camera stutter.
    private Vector3 lastHitPosPlusOffset = Vector3.zero;
    private Vector3 tarHitPosPlusOffset;
    private Vector3 newCombinedRaycastTarget;

    private Vector3 moveSmoothVel;
    private Vector3 camPosBeforePhysCam;
    private Renderer currentObjectRenderer;
    private Renderer ourRendererBeforeLateUpdate;
    private Transform ourTransBeforeLateUpdate;
    private Transform propHolderTrans;
    private Vector3 originPoint;

    public void ReadyCamera(Transform tar, bool result) {
        if (result) {
            target = tar;
            playerReady = true;
            propHolderTrans = target.Find("PropHolder");
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
            dstFromTarget = Mathf.Clamp(dstFromTarget += wheelVal * scrollSpeed, minDist, maxDist);
        }

        if (propHolderTrans != null) {
            if (ourTransBeforeLateUpdate != propHolderTrans.GetChild(0)) {
                ourTransBeforeLateUpdate = propHolderTrans.GetChild(0);
                ourRendererBeforeLateUpdate = ourTransBeforeLateUpdate.GetComponent<Renderer>();

                if (ourRendererBeforeLateUpdate == null) {
                    Debug.LogWarning("Caught instance where our renderer was null on root object. Going one level deeper for a meshrenderer.");
                    ourRendererBeforeLateUpdate = ourTransBeforeLateUpdate.GetChild(0).gameObject.GetComponent<Renderer>();
                }

                Debug.Log("Prop changed?");
            }
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

                // Do we have the latest Renderer for our object? (We use renderer info to get the CENTER point of the object.)
                if (currentObjectRenderer != ourRendererBeforeLateUpdate) {
                    if (ourRendererBeforeLateUpdate != null) {
                        currentObjectRenderer = ourRendererBeforeLateUpdate;
                    } else {
                        Debug.LogWarning("Our renderer is still shooting back with null ref.");
                    }
                }
            }


            RaycastHit hit;
            //Vector3 originPoint = new Vector3(target.position.x, target.position.y + 100f, target.position.z);
            if (currentObjectRenderer != null) {
                originPoint = new Vector3(currentObjectRenderer.bounds.center.x, currentObjectRenderer.bounds.center.y + 100f, currentObjectRenderer.bounds.center.z);
            } else {
                Debug.LogWarning("Still couldn't find meshrenderer one level deeper?...");
            }
            // Are we hitting default layer? (LocalPlayer will always be on default layer.) Is our tag also LocalPlayer?
            if (Physics.Raycast(originPoint, -Vector3.up, out hit, 200f, layerToHit) && (hit.collider.gameObject.transform.root.tag == "LocalPlayer")) {


                tarHitPosPlusOffset = hit.point + yFollowOffset;

                // If it's our first frame cycle, we want to lerp from a relative position. NOT Vec3.zero.
                if (lastHitPosPlusOffset == Vector3.zero) {
                    lastHitPosPlusOffset = gameObject.transform.position + yFollowOffset;
                }

                // We lerp between our raycasts.
                newCombinedRaycastTarget = Vector3.Lerp(lastHitPosPlusOffset, tarHitPosPlusOffset, Time.smoothDeltaTime * lerpBetweenSpeed);

                // Before we actually move the camera, we need to draw from the hit.point to the camera to see if there's something between those points.
                RaycastHit PhysCamHit;
                Debug.DrawRay(hit.point, -gameObject.transform.forward * dstFromTarget, Color.green);
                if (Physics.Raycast(hit.point, -gameObject.transform.forward, out PhysCamHit, dstFromTarget, ~layersToIgnorePhysCam)) {
                    // We hit something. Does it matter what we hit?

                    Debug.Log("TEMP: PhysCam Check -DID- find something between player and camera. Adjusting position. Hit: " + PhysCamHit.collider.gameObject.name);
                    camPosBeforePhysCam = Vector3.SmoothDamp(transform.position, PhysCamHit.point + (gameObject.transform.forward * 3f), ref moveSmoothVel, camLerpSpeed);
                    //transform.position = Vector3.Lerp(gameObject.transform.position, PhysCamHit.point + (gameObject.transform.forward * 3f), Time.deltaTime * hitBlockingObjectLerpSpeed);
                } else {

                    // We lerp between our current pos and target (combined) pos.
                    camPosBeforePhysCam = Vector3.SmoothDamp(transform.position, newCombinedRaycastTarget - transform.forward * dstFromTarget, ref moveSmoothVel, camLerpSpeed);


                    // We didn't hit anything.
                    Debug.Log("TEMP: PhysCam Check didn't find anything between player and camera.");
                }

                // We set the position manually.
                transform.position = camPosBeforePhysCam;

                // We set our lasthit point to the current hit point, so next cycle we'll have that info.
                lastHitPosPlusOffset = tarHitPosPlusOffset;

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
