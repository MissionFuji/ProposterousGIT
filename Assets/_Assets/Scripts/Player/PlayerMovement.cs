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
    private bool isGrounded;
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
    private float groundCheckDist;
    private GameObject ourRaycastTargerObj;
    private string ourPreviousProp;
    private bool rotLocked = false;
    [SerializeField]
    private int takeOverRange;
    private RaycastHit objectHit;
    [SerializeField] private LayerMask PropInteraction;
    private GameObject cursorObj;
    private GameObject LookFollowTar;
    private Transform JumpRaycastOrigin;
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
            JumpRaycastOrigin = new GameObject().transform;
            JumpRaycastOrigin.name = "JumpRaycastObject";
            cursorObj = Camera.main.gameObject.transform.Find("CameraCenter").gameObject;
            LookFollowTar = transform.Find("LookFollowTar").gameObject;
        }
    }


    private void Update() {
        if (pv.IsMine) {

            //Grounded-Checker Object Mapping
            #region
            if (JumpRaycastOrigin != null) {
                JumpRaycastOrigin.transform.position = gameObject.transform.position;
            }
            #endregion

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
                if (rotLocked) {
                    rotLocked = false;
                    photonView.RPC("RPC_UnlockRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
                } else {
                    rotLocked = true;
                    Quaternion rotRef = gameObject.transform.rotation;
                    photonView.RPC("RPC_LockRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID, rotRef);
                }
            }
            if (Input.GetKeyDown(KeyCode.Z)) {
                if (rotLocked) {
                    if (gameObject.transform.rotation != Quaternion.identity) {
                        photonView.RPC("RPC_ResetRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
                    }
                }
            }
            if ((isGrounded) && (Input.GetKeyDown(KeyCode.Space))) {
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
                                if (propInt.isHostOnly) {
                                    if (PhotonNetwork.LocalPlayer.IsMasterClient) {
                                        if (propInt.isMapCartridge) {
                                            mapToLoadName = propInt.gameObject.name;
                                            Debug.Log("Looks like you've become a map cartridge. Map: " + mapToLoadName + ".");
                                        }
                                        if (PPC.moveState != 0 || PPC.moveState != 3) {
                                            //photonView.RPC("RPC_RequestPropPermission", RpcTarget.MasterClient, photonView.ViewID, propInt.gameObject.GetPhotonView().ViewID);
                                            //BecomeProp(objectHit.collider.gameObject);
                                        }
                                    } else {
                                        Debug.Log("Tried to take over a host-only prop and a non-host client.");
                                    }
                                } else { // NOT host-only section.
                                    if (PPC.moveState != 0 || PPC.moveState != 3) {
                                        photonView.RPC("RPC_RequestPropPermission", RpcTarget.MasterClient, pv.ViewID, propInt.gameObject.GetPhotonView().ViewID);
                                        Debug.Log("NEW DAY TEST. LOCAL CLIENT PLAYER TRIED TO TAKE PROP.");
                                        //BecomeProp(objectHit.collider.gameObject);
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
            groundCheckDist = transform.localScale.y * 1.25f;
            Vector3 origin = JumpRaycastOrigin.transform.position;
            Debug.DrawRay(origin, -Vector3.up * groundCheckDist, Color.cyan);
            if (Physics.Raycast(origin, -Vector3.up, groundCheckDist, groudLayer)) {
                isGrounded = true;
            } else {
                isGrounded = false;
            }
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
        Rigidbody tarPlyRB = PhotonView.Find(plyID).gameObject.GetComponent<Rigidbody>();
        tarPlyRB.freezeRotation = false;
    }

    [PunRPC]
    void RPC_LockRotationOverNetwork(int plyID, Quaternion tarRot) {
        PhotonView tarPV = PhotonView.Find(plyID);
        Rigidbody tarPlyRB = tarPV.gameObject.GetComponent<Rigidbody>();
        tarPlyRB.freezeRotation = true;
        tarPV.gameObject.transform.rotation = tarRot;
    }

    [PunRPC]
    void RPC_ResetRotationOverNetwork(int plyID) {
        Transform tarPlyRB = PhotonView.Find(plyID).gameObject.transform;
        tarPlyRB.rotation = Quaternion.identity;
    }

    //Only runs on host.
    [PunRPC]
    void RPC_RequestPropPermission(int plyID, int propID) {
        PhotonView netPV = PhotonView.Find(plyID);
        PhotonView propPV = PhotonView.Find(propID);
        if (propPV.gameObject.GetComponent<PropInteraction>().isAlreadyClaimedOverNetwork == false) {
            Debug.LogError("SUCCESS! TarPlayer: " + netPV.Owner.NickName);
            propPV.gameObject.GetComponent<PropInteraction>().isAlreadyClaimedOverNetwork = true;
         //   photonView.RPC("RPC_BecomePropAfterAuthentication", RpcTarget.All, netPV.ViewID, propPV.ViewID);
        } else {
            Debug.LogError("FAILURE!");
            //we can kickback an RPC here to let the client know he wasn't able to take the prop over.
        }
    }

    //sends to all, executes on target.
    [PunRPC]
    void RPC_BecomePropAfterAuthentication(int plyID, int propID) {
        PhotonView tarPly = PhotonView.Find(plyID);
        PhotonView prop = PhotonView.Find(propID);
        if (tarPly.Owner == pv.Owner) { //Checking if we are tarPly.
            Debug.Log("You were authenticated and were granted the prop over anyone else! Target player: " + tarPly.Owner.NickName);
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
        }
    }

    /*
    void BecomePropFromPreProp() {
        ourRaycastTargerObj = objectHit.collider.gameObject;
        photonView.RPC("RPC_BecomePropFromPreProp", RpcTarget.AllBuffered, ourRaycastTargerObj.GetPhotonView().ViewID, gameObject.GetPhotonView().ViewID);
        ourPreviousProp = "";
        foreach (char c in ourRaycastTargerObj.name) {
            if (System.Char.IsDigit(c)) {
                ourPreviousProp += c;
            }
        }
        PPC.moveState = 2;
    }
    */

    [PunRPC]
    void RPC_BecomePropFromPreProp(int propID, int changingPlyID) {
        GameObject propToRef = PhotonView.Find(propID).gameObject;
        GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
        Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
        plyRB.velocity = Vector3.zero;
        plyRB.isKinematic = true;
        Component.Destroy(changingPly.GetComponent<MeshCollider>());
        changingPly.transform.position = propToRef.transform.position;
        changingPly.GetComponent<MeshFilter>().mesh = propToRef.GetComponent<MeshFilter>().mesh;
        changingPly.GetComponent<MeshRenderer>().material = propToRef.GetComponent<MeshRenderer>().material;
        Quaternion tempRot = propToRef.transform.rotation;
        Vector3 tempScale = propToRef.transform.lossyScale;
        PhysicMaterial tempPhysMat = propToRef.GetComponent<Collider>().material;
        Destroy(propToRef.gameObject);
        changingPly.transform.rotation = tempRot;
        changingPly.transform.localScale = tempScale;
        MeshCollider plyMeshCol = changingPly.AddComponent<MeshCollider>();
        plyMeshCol.material = tempPhysMat;
        plyMeshCol.convex = true;
        if (changingPly.GetComponent<PhotonView>().IsMine) {
            LookFollowTar.transform.position = plyMeshCol.ClosestPoint(LookFollowTar.transform.position);
        }
        plyRB.isKinematic = false;
    }

    /*
    void BecomePropFromProp() {
        ourRaycastTargerObj = objectHit.collider.gameObject;
        photonView.RPC("RPC_BecomePropFromProp", RpcTarget.AllBuffered, ourRaycastTargerObj.GetPhotonView().ViewID, gameObject.GetPhotonView().ViewID, int.Parse(ourPreviousProp));
        ourPreviousProp = "";
        foreach (char c in ourRaycastTargerObj.name) {
            if (System.Char.IsDigit(c)) {
                ourPreviousProp += c;
            }
        }
    }
    */

    [PunRPC]
    void RPC_BecomePropFromProp(int propID, int changingPlyID, int ourOldPropName) {
        GameObject propToRef = PhotonView.Find(propID).gameObject;
        GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
        Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
        Vector3 velRef = plyRB.velocity;
        plyRB.velocity = Vector3.zero;
        plyRB.isKinematic = true;
        Component.Destroy(changingPly.GetComponent<MeshCollider>());
        Vector3 spawnOldPropPos = changingPly.transform.position;
        Quaternion spawnOldPropRot = changingPly.transform.rotation;
        string propName = propToRef.name.Substring(0, 1);
        changingPly.transform.position = propToRef.transform.position;
        changingPly.GetComponent<MeshFilter>().mesh = propToRef.GetComponent<MeshFilter>().mesh;
        changingPly.GetComponent<MeshRenderer>().material = propToRef.GetComponent<MeshRenderer>().material;
        if (changingPly.GetPhotonView().IsMine) {
            GameObject detachingProp = PhotonNetwork.Instantiate("PhotonPrefabs/" + ourOldPropName.ToString(), spawnOldPropPos, spawnOldPropRot);
            Rigidbody detPropRB = detachingProp.GetComponent<Rigidbody>();
            detPropRB.AddForce(velRef * detPropRB.mass, ForceMode.Impulse);
        }
        Quaternion tempRot = propToRef.transform.rotation;
        Vector3 tempScale = propToRef.transform.lossyScale;
        PhysicMaterial tempPhysMat = propToRef.GetComponent<Collider>().material;
        Destroy(propToRef.gameObject);
        changingPly.transform.rotation = tempRot;
        changingPly.transform.localScale = tempScale;
        MeshCollider plyMeshCol = changingPly.AddComponent<MeshCollider>();
        plyMeshCol.material = tempPhysMat;
        plyMeshCol.convex = true;
        if (changingPly.GetComponent<PhotonView>().IsMine) {
            LookFollowTar.transform.position = plyMeshCol.ClosestPoint(LookFollowTar.transform.position);
        }
        plyRB.isKinematic = false;
    }

}


