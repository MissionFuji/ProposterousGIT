using Cinemachine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;


public class PlayerMovement : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    private PhotonView pv;
    [SerializeField]
    private PlayerPropertiesController PPC;
    private float rotSpeed = 0.1f;
    [SerializeField]
    private LayerMask groudLayer;
    [SerializeField]
    private float playerSpeed = 2.0f;
    private GameObject mmc;
    private float turnSmoothVelocity;
    private Rigidbody rb;
    private Transform mmcCamTransRef;
    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private float rotForce;
    private GameObject ourRaycastTargerObj;
    private string ourPreviousProp;
    [SerializeField]
    private bool XYZRotLocked = false;
    private bool XZRotLocked = false;
    [SerializeField]
    private int takeOverRange;
    private RaycastHit objectHit;
    [SerializeField] private LayerMask PropInteraction;
    private GameObject cursorObj;
    private GameObject LookFollowTar;
    private GameObject camTarFor3PC;

    //used only for outline in update.
    [SerializeField]
    private GameObject outlinedObjectRef = null;
    private PropInteraction outlinePropInt = null;
    private Outline ol = null;

    //used for map loading
    public string mapToLoadName;




    private void Start() {
        pv = gameObject.GetComponent<PhotonView>();
        if (pv.IsMine) {
            PPC = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
            mmc = Camera.main.gameObject;
            rb = gameObject.GetComponent<Rigidbody>();
            camTarFor3PC = GameObject.FindGameObjectWithTag("CamFollowTarget");
            camTarFor3PC.GetComponent<CameraTarController>().SetFollowCamTarget(gameObject);
            CinemachineFreeLook cmflRef = gameObject.transform.Find("3PC").GetComponent<CinemachineFreeLook>();
            cmflRef.Follow = camTarFor3PC.transform;
            cmflRef.LookAt = camTarFor3PC.transform;
            mmcCamTransRef = GameObject.FindGameObjectWithTag("mmcCamHelper").transform;
            cursorObj = Camera.main.gameObject.transform.Find("CameraCenter").gameObject;
            LookFollowTar = transform.Find("LookFollowTar").gameObject;
        }
    }


    private void Update() {
        if (pv.IsMine) {

            //Highlighting Code
            #region
            Vector3 fwd = cursorObj.transform.TransformDirection(Vector3.forward);
            Debug.DrawRay(cursorObj.transform.position, fwd * 120f, Color.magenta);
            if (Physics.Raycast(cursorObj.transform.position, fwd, out objectHit, 120f, PropInteraction)) {
                if (Vector3.Distance(objectHit.collider.gameObject.transform.position, gameObject.transform.position) <= takeOverRange) {
                    if (objectHit.collider.gameObject.layer == 11) { // if it's a regular prop.
                        GameObject hoveredObject = objectHit.collider.gameObject;
                        if (outlinedObjectRef != null) {
                            if (outlinedObjectRef != hoveredObject) {
                                outlinedObjectRef.GetComponent<Outline>().enabled = false;
                            }
                        }
                        if (outlinedObjectRef != hoveredObject) {
                            outlinedObjectRef = hoveredObject;
                            ol = hoveredObject.GetComponent<Outline>();
                            outlinePropInt = hoveredObject.GetComponent<PropInteraction>();
                        }
                        if (outlinePropInt.isAvailable) {
                            if (ol.enabled != true) {
                                ol.enabled = true;
                            }
                            if (!outlinePropInt.isHostOnly) {
                                if (ol.OutlineColor != Color.white) {
                                    ol.OutlineColor = Color.white;
                                }
                            } else {
                                if (PhotonNetwork.LocalPlayer.IsMasterClient) {
                                    if (ol.OutlineColor != Color.white) {
                                        ol.OutlineColor = Color.white;
                                    }
                                } else {
                                    if (ol.OutlineColor != Color.red) {
                                        ol.OutlineColor = Color.red;
                                    }
                                }
                            }
                        } else {
                            if (ol.enabled || ol.OutlineColor != Color.red) {
                                ol.enabled = true;
                                ol.OutlineColor = Color.red;
                            }
                        }

                    } else if (objectHit.collider.gameObject.layer == 13) { // if it's a player.
                        GameObject hoveredObject = objectHit.collider.gameObject;
                        if (outlinedObjectRef != null) {
                            if (outlinedObjectRef != hoveredObject) {
                                outlinedObjectRef.GetComponent<Outline>().enabled = false;
                            }
                        }
                        if (outlinedObjectRef != hoveredObject) {
                            outlinedObjectRef = hoveredObject;
                            ol = hoveredObject.GetComponent<Outline>();
                            outlinePropInt = hoveredObject.GetComponent<PropInteraction>();
                        }
                        if (ol.enabled == false || ol.OutlineColor != Color.red) {
                            ol.enabled = true;
                            ol.OutlineColor = Color.red;
                            Debug.Log("Trying to highlight player object.");
                        }

                    }
                } else {
                    if (outlinedObjectRef != null) {
                        if (outlinedObjectRef.GetComponent<Outline>().enabled == true) {
                            outlinedObjectRef.GetComponent<Outline>().enabled = false;
                            Debug.Log("Left range. reset");
                        }
                    }
                }
            } else {
                if (outlinedObjectRef != null) {
                    if (outlinedObjectRef.GetComponent<Outline>().enabled == true) {
                        outlinedObjectRef.GetComponent<Outline>().enabled = false;
                        Debug.Log("Left moved cursor outside of object's hitbox. reset");
                    }
                }
            }
            #endregion

            if (Input.GetKeyDown(KeyCode.R)) {
                if (XYZRotLocked) {
                    XYZRotLocked = false;
                    photonView.RPC("RPC_UnlockRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
                } else {
                    XYZRotLocked = true;
                    Quaternion rotRef = gameObject.transform.rotation;
                    photonView.RPC("RPC_LockRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID, rotRef);
                }
            }
            if (Input.GetKeyDown(KeyCode.Z)) {
                if (XYZRotLocked) {
                    if (gameObject.transform.rotation != Quaternion.identity) {
                        photonView.RPC("RPC_ResetRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Space)) {
                if (PPC.moveState != 0 || PPC.moveState != 4) { // Make sure we're not frozen or dead.
                    rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z); // Jump Code. This had to go into Update due to Input.GetKeyDown.
                    rb.AddTorque(new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10)), ForceMode.Impulse);
                    Debug.Log("JUMP!");
                }
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                Debug.DrawRay(cursorObj.transform.position, fwd * 120f, Color.green);
                if (Physics.Raycast(cursorObj.transform.position, fwd, out objectHit, 120f, PropInteraction)) {
                    if (Vector3.Distance(objectHit.collider.gameObject.transform.position, gameObject.transform.position) <= takeOverRange) {
                        PropInteraction propInt;
                        if (objectHit.collider.gameObject.GetComponent<PropInteraction>()) {
                            propInt = objectHit.collider.gameObject.GetComponent<PropInteraction>();
                            if (propInt.isAvailable) { // if it does, let's see if prop is available.
                                if (propInt.isHostOnly) { // Are we the host for this host-only selection?
                                    if (PhotonNetwork.LocalPlayer.IsMasterClient) {
                                        if (propInt.isMapCartridge) {
                                            mapToLoadName = propInt.gameObject.name;
                                            Debug.Log("Looks like you've become a map cartridge. Map: " + mapToLoadName + ".");
                                        }
                                        if (PPC.moveState != 0 || PPC.moveState != 3) {
                                            BecomeProp(pv.ViewID, propInt.gameObject.GetPhotonView().ViewID);
                                        }
                                    } else {
                                        Debug.Log("Tried to take over a host-only prop and a non-host client.");
                                    }
                                } else { // NOT host-only section.
                                    if (PPC.moveState != 0 || PPC.moveState != 3) {
                                        if (pv.ViewID != 0 && propInt.gameObject.GetPhotonView() != null && propInt.gameObject != null) {
                                            BecomeProp(pv.ViewID, propInt.gameObject.GetPhotonView().ViewID);
                                        } else {
                                            Debug.LogError("When performing takeover, the target prop became unavailable for unexpected reasons.");
                                        }
                                    }
                                }

                            } else if (!propInt.isAvailable) {
                                Debug.Log("As a pre-prop, you tried to possess: " + objectHit.collider.gameObject.name + ", failed takeover. Prop already posessed by another player.");
                            }
                        } else {
                            Debug.Log("As a pre-prop, you tried to possess: " + objectHit.collider.gameObject.name + ", failed takeover. Prop is not Posessable.");
                        }
                    }
                }
            }

        }

    }

    void FixedUpdate() {
        if (pv.IsMine == true) {
            // Movement Code.
            #region
            if (PPC.moveState != 0) { // 0 = Frozen
                if (PPC.moveState == 1) { // 1 = Pre-Prop

                    float xDir = Input.GetAxisRaw("Horizontal") * playerSpeed;
                    float yDir = Input.GetAxisRaw("Vertical") * playerSpeed;
                    mmcCamTransRef.eulerAngles = new Vector3(0, mmc.transform.eulerAngles.y, 0);
                    float targetAngle = Mathf.Atan2(xDir, yDir) * Mathf.Rad2Deg + mmc.transform.eulerAngles.y;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotSpeed);
                    if ((xDir != 0f) || (yDir != 0f)) {
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    }
                    rb.AddForce(Physics.gravity * (rb.mass * rb.mass));
                    Vector3 movePos = mmc.transform.right * xDir + mmcCamTransRef.forward * yDir;
                    Vector3 newMovePos = new Vector3(movePos.x, rb.velocity.y, movePos.z);
                    rb.velocity = newMovePos;

                } else if (PPC.moveState == 2) { // 2 = Prop
                    float xDir = Input.GetAxisRaw("Horizontal") * playerSpeed;
                    float yDir = Input.GetAxisRaw("Vertical") * playerSpeed;
                    mmcCamTransRef.eulerAngles = new Vector3(0, mmc.transform.eulerAngles.y, 0);
                    float targetAngle = Mathf.Atan2(xDir, yDir) * Mathf.Rad2Deg + mmc.transform.eulerAngles.y;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotSpeed);
                    rb.AddForce(Physics.gravity * (rb.mass * rb.mass));
                    Vector3 movePos = mmc.transform.right * xDir + mmcCamTransRef.forward * yDir;
                    Vector3 newMovePos = new Vector3(movePos.x, rb.velocity.y, movePos.z);

                    if (xDir != 0 || yDir != 0) {
                        rb.velocity = newMovePos;
                    } else {
                        rb.velocity = rb.velocity;
                    }
                    /*
                    if (!rotLocked) {
                        if (xDir != 0 && yDir != 0) {
                            rb.AddTorque((newMoveRot) * rotForce, ForceMode.Force);    *****DISABLE DUE TO UNREALISTIC BEHAVIOR*****
                        }
                    }
                    */

                } else if (PPC.moveState == 3) { // 3 = Seeker

                } else if (PPC.moveState == 4) { // 4 = Ghost/Spec
                    Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

                    //X/Y Movement.
                    if (direction.magnitude >= 0.1f) {
                        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mmc.transform.eulerAngles.y;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotSpeed);

                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                        //controller.Move(moveDir.normalized * playerSpeed * Time.deltaTime);
                    }

                    //Z Movement.
                    if (Input.GetKey(KeyCode.Space) && (!Input.GetKey(KeyCode.LeftShift))) {
                        //controller.Move(Vector3.up * (playerSpeed / 2) * Time.deltaTime);
                    } else if (!Input.GetKey(KeyCode.Space) && (Input.GetKey(KeyCode.LeftShift))) {
                        //controller.Move(-Vector3.up * (playerSpeed / 2) * Time.deltaTime);
                    }
                }
            }
            #endregion 
        }
    }


    [PunRPC]
    void RPC_UnlockRotationOverNetwork(int plyID) {
        Rigidbody targetRB = PhotonView.Find(plyID).gameObject.GetComponent<Rigidbody>();
        targetRB.freezeRotation = false;
    }

    [PunRPC]
    void RPC_LockRotationOverNetwork(int plyID, Quaternion tarRot) {
        PhotonView tarPV = PhotonView.Find(plyID);
        Rigidbody targetRB = tarPV.gameObject.GetComponent<Rigidbody>();
        targetRB.freezeRotation = true;
        tarPV.gameObject.transform.rotation = tarRot;
    }

    [PunRPC]
    void RPC_ResetRotationOverNetwork(int plyID) {
        Transform targetPly = PhotonView.Find(plyID).gameObject.transform.GetChild(0);
        targetPly.rotation = Quaternion.identity;
    }


    void BecomeProp(int plyID, int propID) {
        PhotonView tarPly = PhotonView.Find(plyID);
        PhotonView prop = PhotonView.Find(propID);
        if (prop.gameObject != null && tarPly.gameObject != null) {
            if (PPC.moveState == 1) {
                ourRaycastTargerObj = prop.gameObject;
                photonView.RPC("RPC_BecomePropFromPreProp", RpcTarget.AllBuffered, ourRaycastTargerObj.GetPhotonView().ViewID, gameObject.GetPhotonView().ViewID);
                ourPreviousProp = "";
                foreach (char c in ourRaycastTargerObj.name) {
                    if (System.Char.IsDigit(c)) {
                        ourPreviousProp += c;
                    }
                }
                PPC.moveState = 2;
            } else if (PPC.moveState == 2) {
                ourRaycastTargerObj = prop.gameObject;
                photonView.RPC("RPC_BecomePropFromProp", RpcTarget.AllBuffered, ourRaycastTargerObj.GetPhotonView().ViewID, gameObject.GetPhotonView().ViewID, int.Parse(ourPreviousProp));
                ourPreviousProp = "";
                foreach (char c in ourRaycastTargerObj.name) {
                    if (System.Char.IsDigit(c)) {
                        ourPreviousProp += c;
                    }
                }
            }
        } else {
            Debug.LogError("The prop or target player you're referencing is null. Maybe prop was taken?");
        }
    }


    [PunRPC]
    void RPC_BecomePropFromPreProp(int propID, int changingPlyID) {
        //Store data for current prop/player.
        GameObject targetPropRef = PhotonView.Find(propID).gameObject;
        GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
        GameObject propHolder = changingPly.transform.Find("PropHolder").gameObject;
        Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
        Rigidbody targetPropRB = targetPropRef.GetComponent<Rigidbody>();

        // Let's make sure our prop is still active after sending this command over the network.
        if (targetPropRef != null) {

            //Set the object to unavailable over the network.
            targetPropRef.gameObject.GetComponent<PropInteraction>().isAvailable = false;
            //Let's make sure that our targetProp has no rigidbody. Also disabling network tracking of its velocity. CAN NOT SET isKinematic! This causes collider to not work! Must destroy RB!
            targetPropRef.GetPhotonView().ObservedComponents.Clear();
            targetPropRef.GetComponent<RigidbodyTransformView>().enabled = false;
            Destroy(targetPropRB);
            //We can DESTROY our current child object because it is pre-prop. We don't want to leave this one behind anywhere.
            foreach (Transform child in propHolder.transform) {
                Destroy(child.gameObject);
            }
            //Let's temporaribly freeze our player and set move it to the target prop position before takeover.
            plyRB.velocity = Vector3.zero;
            plyRB.isKinematic = true;
            changingPly.transform.position = targetPropRef.transform.position;
            //We set our layer to 0 so we don't highlight ourselves while we are a prop.
            if (pv.IsMine) {
                targetPropRef.layer = 0;
            }
            //Let's ref some data from our target prop before takeover so we can apply changes to it.
            Quaternion tempRot = targetPropRef.transform.rotation;
            Vector3 tempScale = targetPropRef.transform.lossyScale;
            //Prop takeover, parent, then apply transforms to it.
            targetPropRef.transform.parent = propHolder.transform;
            targetPropRef.transform.rotation = tempRot;
            targetPropRef.transform.localScale = tempScale;
            targetPropRef.transform.localPosition = Vector3.zero;
            //re-enable rigidbody so we can move around again.
            plyRB.isKinematic = false;
        } else {
            Debug.LogError("The prop we tried to take over was taken! We can try here to rectify this issue.");
        }
    }


    [PunRPC]
    void RPC_BecomePropFromProp(int propID, int changingPlyID, int ourOldPropName) {
        //Store data for current prop/player.
        GameObject targetProp = PhotonView.Find(propID).gameObject;
        GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
        GameObject propHolder = changingPly.transform.Find("PropHolder").gameObject;
        Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
        Rigidbody targetPropRB = targetProp.GetComponent<Rigidbody>();
        Vector3 velRef = plyRB.velocity;
        Vector3 velAngVel = plyRB.angularVelocity;
        float massRef = plyRB.mass;
        //Clear un-needed network calls on photonview.
        targetProp.GetPhotonView().ObservedComponents.Clear();
        targetProp.GetComponent<RigidbodyTransformView>().enabled = false;
        //Destroy rigidbody before we parent this object.
        Destroy(targetPropRB);
        //freeze our player just before the swap.
        plyRB.velocity = Vector3.zero;
        plyRB.isKinematic = true;
        //set spawn pos and rot of current child object.
        Vector3 spawnOldPropPos = propHolder.transform.position;
        Quaternion spawnOldPropRot = propHolder.transform.rotation;
        //Now we detach our current prop and unparent it.
        int childrenDetached = 0;
        GameObject detachingProp = null;
        foreach (Transform child in propHolder.transform) {
            child.parent = null;
            detachingProp = child.gameObject;
            detachingProp.layer = 11;
            if (!detachingProp.GetComponent<Rigidbody>()) {
                detachingProp.AddComponent<Rigidbody>(); //re-adding rb to detaching prop.
            }
            detachingProp.GetComponent<PropInteraction>().isAlreadyClaimedOverNetwork = false;
            childrenDetached++;
            if (childrenDetached > 1) {
                Debug.LogWarning("We detached all children from the player's PropHolder. But there was more than one?");
            }
        }
        Rigidbody detPropRB = detachingProp.GetComponent<Rigidbody>();
        RigidbodyTransformView rtv = detPropRB.GetComponent<RigidbodyTransformView>();
        rtv.enabled = true;
        detPropRB.gameObject.GetPhotonView().ObservedComponents.Add(rtv);
        detPropRB.gameObject.GetComponent<PropInteraction>().ResetRigidBodyAfterDetach();
        detPropRB.isKinematic = false;
        detPropRB.mass = massRef;
        detPropRB.AddForce(velRef * detPropRB.mass, ForceMode.Impulse);
        detPropRB.AddTorque(velAngVel * detPropRB.mass, ForceMode.Impulse);
        //Now we move our player to the target prop location and set references to size and rotation of object.
        changingPly.transform.position = targetProp.transform.position;
        Quaternion tempRot = targetProp.transform.rotation;
        Vector3 tempScale = targetProp.transform.lossyScale;
        //We set our layer to 0 so we don't highlight ourselves while we are a prop.
        if (pv.IsMine) {
            targetProp.layer = 0;
        }
        //apply all transform after we parent to keep everything accurate.
        targetProp.transform.parent = propHolder.transform;
        targetProp.transform.rotation = tempRot;
        targetProp.transform.localScale = tempScale;
        targetProp.transform.localPosition = Vector3.zero;
        //Re-enable our rigidbody so we can move around again.
        plyRB.isKinematic = false;
    }

}


