using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


public class PlayerMovement : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    // Modifiable in editor, invisible to cheaters.
    [SerializeField]
    private float rotSpeed = 0.1f;
    [SerializeField]
    private LayerMask groudLayer;
    [SerializeField]
    private float playerSpeed = 2.0f;
    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private float rotForce;
    [SerializeField]
    private bool RotLocked = false;
    [SerializeField]
    private int takeOverRange;
    [SerializeField]
    private LayerMask PropInteraction;

    private PhotonView pv;
    private CameraController camController;
    private PlayerPropertiesController PPC;
    private GameObject mmc;
    private float turnSmoothVelocity;
    private Rigidbody rb;
    private Transform mmcCamTransRef;
    private GameObject ourRaycastTargerObj;
    private string ourPreviousProp;
    private RaycastHit objectHit;
    private GameObject cursorObj;

    //used only for outline in update.
    [SerializeField]
    private GameObject outlinedObjectRef = null;
    private PropInteraction outlinePropInt = null;
    private Outline ol = null;
    private List<GameObject> highlightList = new List<GameObject>();

    //used for map loading
    public string mapToLoadName;

    //used for anti-dupe.
    private GameObject propWeTryToTake = null;
    private string propWeTryToTakeName = "";

    //used for smooth move in update. rotation online.
    private float xDir;
    private float yDir;
    private float angle;
    private float targetAngle;
    private GameObject rotPropHolder;

    //use for lock/unlock rot image.

    public Sprite lockedSprite;
    public Sprite unlockedSprite;
    private GameObject rootCanvas;
    private Image rotLockImg;


    // recursive search for children of children for highlighting.
    private void AddDescendantsWithTag(Transform parent, List<GameObject> list) {
        foreach (Transform child in parent) {
            if (child.gameObject.GetComponent<Outline>()) {
                list.Add(child.gameObject);
            }
            AddDescendantsWithTag(child, list);
        }
    }

    private void Start() {
        pv = gameObject.GetComponent<PhotonView>();
        if (pv.IsMine) {
            PPC = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
            mmc = Camera.main.gameObject;
            rootCanvas = GameObject.FindGameObjectWithTag("RootCanvas");
            rotLockImg = rootCanvas.transform.Find("RoomUI/LockState").gameObject.GetComponent<Image>();
            rotPropHolder = gameObject.transform.Find("PropHolder").gameObject;
            camController = mmc.GetComponent<CameraController>();
            rb = gameObject.GetComponent<Rigidbody>();
            mmcCamTransRef = GameObject.FindGameObjectWithTag("mmcCamHelper").transform; // this is what gives us accurate y rotation for player.
            mmcCamTransRef.GetComponent<CameraTarController>().SetCamFollowToPlayer(this.gameObject);
            camController.ReadyCamera(gameObject.transform);
            cursorObj = mmc.transform.GetChild(0).gameObject;
        }
    }


    private void Update() {
        if (pv.IsMine) {
            xDir = Input.GetAxisRaw("Horizontal") * playerSpeed;
            yDir = Input.GetAxisRaw("Vertical") * playerSpeed;
            mmcCamTransRef.eulerAngles = new Vector3(0, mmc.transform.eulerAngles.y, 0);
            targetAngle = Mathf.Atan2(xDir, yDir) * Mathf.Rad2Deg + mmc.transform.eulerAngles.y;
            if (PPC.moveState == 1) {
                if (rotPropHolder.transform.childCount > 0) {
                    GameObject prop = rotPropHolder.transform.GetChild(0).gameObject;
                    prop.transform.rotation = Quaternion.Euler(prop.transform.rotation.x, mmcCamTransRef.eulerAngles.y, prop.transform.rotation.z);
                }
            } else if (PPC.moveState == 2) {
                if (rotPropHolder.transform.childCount > 0) {
                    if (RotLocked) {
                        if ((xDir != 0f) || (yDir != 0f)) {
                            GameObject prop = rotPropHolder.transform.GetChild(0).gameObject;
                            prop.transform.rotation = Quaternion.Euler(prop.transform.rotation.x, mmcCamTransRef.eulerAngles.y, prop.transform.rotation.z);
                        }
                    }
                }
            }


            //Highlighting Code
            #region
            Vector3 fwd = cursorObj.transform.TransformDirection(Vector3.forward);
            Debug.DrawRay(cursorObj.transform.position, fwd * 120f, Color.magenta);
            if (Physics.Raycast(cursorObj.transform.position, fwd, out objectHit, 120f, PropInteraction)) {
                if (Vector3.Distance(objectHit.collider.gameObject.transform.position, gameObject.transform.position) <= takeOverRange) {
                    if (objectHit.collider.gameObject.layer == 11) { // if it's a regular prop.
                        GameObject hoveredObject = objectHit.collider.gameObject;

                        //Beginning of each frame, Let's check to see if we're hovering a new object. If not, let's clear last highlight.
                        if (outlinedObjectRef != null) {
                            if (outlinedObjectRef != hoveredObject) {
                                foreach (GameObject obj in highlightList) {
                                    if (obj != null)
                                        obj.GetComponent<Outline>().enabled = false;
                                }
                                highlightList.Clear();
                            }
                        }

                        //If we're hovering an object that isn't currently highlighted, let's set up to highlight it.
                        if (outlinedObjectRef != hoveredObject) {
                            outlinedObjectRef = hoveredObject;
                            highlightList.Add(outlinedObjectRef);
                            outlinePropInt = hoveredObject.GetComponent<PropInteraction>();
                            AddDescendantsWithTag(outlinedObjectRef.transform, highlightList);
                        }


                        foreach (GameObject obj in highlightList) {
                            if (obj != null) {
                                ol = obj.GetComponent<Outline>();
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
                                    if (ol.enabled != true) {
                                        ol.enabled = true;
                                    }
                                    if (ol.OutlineColor != Color.red) {
                                        ol.OutlineColor = Color.red;
                                    }
                                }
                            }
                        }



                    }
                } else {
                    if (outlinedObjectRef != null) {
                        foreach (GameObject obj in highlightList) {
                            if (obj != null)
                                obj.GetComponent<Outline>().enabled = false;
                        }
                    }
                }
            } else {
                if (outlinedObjectRef != null) {
                    foreach (GameObject obj in highlightList) {
                        if (obj != null)
                            obj.GetComponent<Outline>().enabled = false;
                    }
                }
            }
            #endregion

            if (Input.GetKeyDown(KeyCode.R)) {
                if (PPC.moveState == 2) {
                    if (RotLocked) {
                        RotLocked = false;
                        gameObject.GetComponent<RigidbodyTransformView>().isRotLocked = false;
                        rotLockImg.sprite = unlockedSprite;
                        photonView.RPC("RPC_UnlockRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
                    } else {
                        RotLocked = true;
                        gameObject.GetComponent<RigidbodyTransformView>().isRotLocked = true;
                        rotLockImg.sprite = lockedSprite;
                        photonView.RPC("RPC_LockRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Z)) {
                if (RotLocked) {
                    photonView.RPC("RPC_ResetRotationOverNetwork", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID);
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
                                            propWeTryToTake = propInt.gameObject;
                                            propWeTryToTakeName = propWeTryToTake.name;
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
                    rb.AddForce(Physics.gravity * (rb.mass * rb.mass));
                    Vector3 movePos = mmcCamTransRef.right * xDir + mmcCamTransRef.forward * yDir;
                    Vector3 newMovePos = new Vector3(movePos.x, rb.velocity.y, movePos.z);
                    rb.velocity = newMovePos;

                } else if (PPC.moveState == 2) { // 2 = Prop
                    rb.AddForce(Physics.gravity * (rb.mass * rb.mass));
                    Vector3 movePos = mmcCamTransRef.right * xDir + mmcCamTransRef.forward * yDir;
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
        Rigidbody plyRB = PhotonView.Find(plyID).gameObject.GetComponent<Rigidbody>();
        plyRB.freezeRotation = false;
    }

    [PunRPC]
    void RPC_LockRotationOverNetwork(int plyID) {
        PhotonView plyPV = PhotonView.Find(plyID);
        Rigidbody plyRB = plyPV.gameObject.GetComponent<Rigidbody>();
        plyRB.freezeRotation = true;
    }

    [PunRPC]
    void RPC_ResetRotationOverNetwork(int plyID) {
        GameObject targetPly = PhotonView.Find(plyID).gameObject;
        Transform targetPlyProp = targetPly.transform.Find("PropHolder").GetChild(0);
        Rigidbody targetPlyRigidbody = targetPly.GetComponent<Rigidbody>();
        targetPlyRigidbody.isKinematic = true;
        targetPly.transform.rotation = Quaternion.identity;
        targetPlyProp.gameObject.transform.rotation = Quaternion.identity;
        targetPlyRigidbody.isKinematic = false;
    }


    void BecomeProp(int plyID, int propID) {
        PhotonView tarPly = PhotonView.Find(plyID);
        PhotonView prop = PhotonView.Find(propID);
        if (prop.gameObject != null && tarPly.gameObject != null) {
            if (PPC.moveState == 1) {
                ourRaycastTargerObj = prop.gameObject;
                string backupTargetPropName = "";
                foreach (char c in ourRaycastTargerObj.name) { // this is purely for backup duplication purposes.
                    if (System.Char.IsDigit(c)) {
                        backupTargetPropName += c;
                    }
                }
                photonView.RPC("RPC_BecomePropFromPreProp", RpcTarget.AllBuffered, ourRaycastTargerObj.GetPhotonView().ViewID, gameObject.GetPhotonView().ViewID, int.Parse(backupTargetPropName));
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
    void RPC_BecomePropFromPreProp(int propID, int changingPlyID, int targetPropBackup) {

        PhotonView tarPropPV = PhotonView.Find(propID);
        if (tarPropPV != null) {
            GameObject targetPropRef = PhotonView.Find(propID).gameObject;
            GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
            GameObject propHolder = changingPly.transform.Find("PropHolder").gameObject;
            Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
            Rigidbody targetPropRB = targetPropRef.GetComponent<Rigidbody>();
            GameObject newNetworkProp = null;
            string tarPropName = targetPropRef.name;
            Quaternion propTempRot = targetPropRef.transform.rotation;
            Vector3 propTempPos = targetPropRef.transform.position;


            //We can DESTROY our current child object because it is pre-prop. We don't want to leave this one behind anywhere.
            foreach (Transform child in propHolder.transform) {
                Destroy(child.gameObject);
            }
            //Let's temporarily freeze our player and set move it to the target prop position before takeover.
            plyRB.velocity = Vector3.zero;
            plyRB.isKinematic = true;
            changingPly.transform.position = propTempPos;
            // Let's spawn our object PER client, not PN.Inst(). This is because we can't reference new GO on all clients. So we must instantiate separately.
            string propToSpawn = "";
            foreach (char c in tarPropName) {
                if (System.Char.IsDigit(c)) {
                    propToSpawn += c;
                }
            }
            Destroy(targetPropRef);

            //We set RotLocked to false so when we become a prop, our rotation is organically unlocked.
            if (changingPly.GetPhotonView().Owner.IsLocal) { //If we are the local player AND the targetPlayer.
                RotLocked = false;
            }

            // We need to store object data and send it through photonnetwork.instantiate because we need to reference out player.
            object[] InitData = new object[]
            {
                changingPly.GetPhotonView().ViewID
            };

            //Let's spawn a prop now that we detached the old prop.
            if (PhotonNetwork.IsMasterClient) { //If we are the MasterClient. We need to instantiate ALL props from MC, as they get deleted when the client leaves if they spawn them in.
                newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", propToSpawn), propTempPos, propTempRot, 0, InitData);
                //We disable our Locked rotation so that we when we take over our new prop, we won't be locked.
            }
            // The rest gets handled from the callback created by instantiating this object. This code is on the PropInteraction Script on prop object.
        } else {
            Debug.LogWarning("PROP TAKEOVER FAILSAFE: Prop you tried to take was unavailable. Creating a copy for you.");

            //Setup needed variables.
            GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
            GameObject propHolder = changingPly.transform.Find("PropHolder").gameObject;
            Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
            GameObject newNetworkProp = null;

            //We can DESTROY our current child object because it is pre-prop. We don't want to leave this one behind anywhere.
            foreach (Transform child in propHolder.transform) {
                Destroy(child.gameObject);
            }
            //Let's temporarily freeze our player and keep it where it is. This is due to a dupe.
            plyRB.velocity = Vector3.zero;
            plyRB.isKinematic = true;

            //We need to use a backup string to instantiate our prop, as it was destroyed or stolen from us.
            string propToSpawn = targetPropBackup.ToString();

            //We set RotLocked to false so when we become a prop, our rotation is organically unlocked.
            if (changingPly.GetPhotonView().Owner.IsLocal) { //If we are the local player AND the targetPlayer.
                RotLocked = false;
            }

            // We need to store object data and send it through photonnetwork.instantiate because we need to reference out player.
            object[] InitData = new object[]
            {
                changingPly.GetPhotonView().ViewID
            };

            //Let's spawn a prop now that we detached the old prop.
            if (PhotonNetwork.IsMasterClient) { //If we are the MasterClient. We need to instantiate ALL props from MC, as they get deleted when the client leaves if they spawn them in.
                newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", propToSpawn), gameObject.transform.position, Quaternion.identity, 0, InitData);
                //We disable our Locked rotation so that we when we take over our new prop, we won't be locked.
            }
            // The rest gets handled from the callback created by instantiating this object. This code is on the PropInteraction Script on prop object.
        }
    }


    [PunRPC]
    void RPC_BecomePropFromProp(int propID, int changingPlyID, int ourOldPropName) {
        //Store data for current prop/player.
        PhotonView tarPropPV = PhotonView.Find(propID);
        if (tarPropPV != null) {
            //Setup needed vars.
            GameObject targetProp = tarPropPV.gameObject;
            GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
            GameObject propHolder = changingPly.transform.Find("PropHolder").gameObject;
            GameObject newNetworkProp = null;
            Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();
            Rigidbody targetPropRB = targetProp.GetComponent<Rigidbody>();
            Vector3 velRef = plyRB.velocity;
            Vector3 velAngRef = plyRB.angularVelocity;
            Quaternion propTempRot = targetProp.transform.rotation;
            Vector3 propTempPos = targetProp.transform.position;
            float massRef = plyRB.mass;
            string tarPropName = targetProp.name;

            //Clear un-needed network calls on photonview.
            targetProp.GetPhotonView().ObservedComponents.Clear();
            targetProp.GetComponent<PropRigidbodyTransformView>().enabled = false;

            //Destroy rigidbody before we parent this object.
            Destroy(targetPropRB);

            //freeze our player just before the swap.
            plyRB.velocity = Vector3.zero;
            plyRB.isKinematic = true;

            //Now we detach our current prop and unparent it.
            int childrenDetached = 0;
            GameObject detachingProp = null;

            foreach (Transform child in propHolder.transform) {
                //Unparent Object.
                child.parent = null;
                //Add reference to detaching object.
                detachingProp = child.gameObject;
                //re-add the object to propInteraction layer for highlights.
                detachingProp.layer = 11;
                //Reset the tag to untagged. Because if it was attached, the tag is likely "AttachedProp" which we don't want on a detached prop.
                detachingProp.tag = "Untagged";
                // Check to see if the prop doesn't have a rigidbody before we fully detach. (At this point, prop SHOULD NOT have a rigidbody. We should add one before detach) 
                if (!detachingProp.GetComponent<Rigidbody>()) {

                    //re-adding rb to detaching prop.
                    detachingProp.AddComponent<Rigidbody>();
                    Rigidbody detPropRB = detachingProp.GetComponent<Rigidbody>();

                    //Make sure we re-enable the networking script directly.
                    PropRigidbodyTransformView prtv = detPropRB.GetComponent<PropRigidbodyTransformView>();
                    if (prtv != null) {
                        prtv.enabled = true;
                    } else {
                        Debug.LogError("Detaching prop did not have a PRTV on it!");
                    }

                    //Make a reference to the PV on the object.
                    PhotonView detachPropPV = detPropRB.GetComponent<PhotonView>();
                    //Ensure the prop's PV is observing and update position/rot/physics over the network.
                    detachPropPV.ObservedComponents.Add(prtv);

                    //We need to make sure the masterclient "owns" these detached props via PhotonView. So we can have better cleanup when the round ends.
                    if (PhotonNetwork.LocalPlayer.IsMasterClient) {
                        detachPropPV.RequestOwnership();
                    }

                    //Run a function on the PropInteraction Script to make sure RigidBody is enabled.
                    detPropRB.gameObject.GetComponent<PropInteraction>().ResetRigidBodyAfterDetach();
                    //Apply RB momentum and velocities and unfreeze RB before we do that.
                    detPropRB.isKinematic = false;
                    detPropRB.mass = massRef;
                    detPropRB.AddForce(velRef * detPropRB.mass, ForceMode.Impulse);
                    detPropRB.AddTorque(velAngRef * detPropRB.mass, ForceMode.Impulse);
                } else {
                    Debug.LogError("The prop we're trying to detach already has a rigidbody. This is an issue that needs to be fixed.");
                }

                //Set prop to be available for takeover across the network.
                detachingProp.GetComponent<PropInteraction>().isAvailable = true;
                //Track how many children we detach. If we find ourselves detaching more than one child, that's an issue.
                childrenDetached++;
                if (childrenDetached > 1) {
                    Debug.LogWarning("We detached all children from the player's PropHolder. But there was more than one?");
                }

            }
            //Now we move our player to the target prop location. Player has already been "frozen".
            changingPly.transform.position = targetProp.transform.position;
            //Gotta have ref to prop name after it is destroyed.
            string propToSpawn = "";
            foreach (char c in tarPropName) {
                if (System.Char.IsDigit(c)) {
                    propToSpawn += c;
                }
            }
            //Destroy the target prop we're attempting to take over.
            Destroy(targetProp);

            //We set RotLocked to false so when we become a prop, our rotation is organically unlocked.
            if (changingPly.GetPhotonView().Owner.IsLocal) { //If we are the local player AND the targetPlayer.
                RotLocked = false;
            }

            // We need to store object data and send it through photonnetwork.instantiate because we need to reference out player.
            object[] InitData = new object[]
            {
                changingPly.GetPhotonView().ViewID
            };

            //Let's spawn a prop now that we detached the old prop.
            if (PhotonNetwork.IsMasterClient) { //If we are the MasterClient. We need to instantiate ALL props from MC, as they get deleted when the client leaves if they spawn them in.
                newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", propToSpawn), propTempPos, propTempRot, 0, InitData);
                //We disable our Locked rotation so that we when we take over our new prop, we won't be locked.
            }
            //The rest gets handled on a callback from photon instantiation.
        } else {
            Debug.LogWarning("PROP TAKEOVER FAILSAFE: Prop you tried to take was unavailable. Creating a copy for you.");

            //Setup initially needed vars.
            GameObject changingPly = PhotonView.Find(changingPlyID).gameObject;
            GameObject propHolder = changingPly.transform.Find("PropHolder").gameObject;
            GameObject newNetworkProp = null;
            Rigidbody plyRB = changingPly.GetComponent<Rigidbody>();

            //Keep velocity info to later apply it to the detachingprop.
            Vector3 velRef = plyRB.velocity;
            Vector3 velAngRef = plyRB.angularVelocity;
            float massRef = plyRB.mass;

            //freeze our player just before the swap.
            plyRB.velocity = Vector3.zero;
            plyRB.isKinematic = true;

            //We're going to need to count how many props we detach and make a var for them.
            int childrenDetached = 0;
            GameObject detachingProp = null;
            //Loop through all children props, we really should only have one. BUT, just in-case...
            foreach (Transform child in propHolder.transform) {
                //Unparent prop.
                child.parent = null;
                //Set reference to detaching prop.
                detachingProp = child.gameObject;
                //Set prop layer to PropInteraction Layer to make sure we highlight it after it's detached.
                detachingProp.layer = 11;
                //Reset the tag to untagged. Because if it was attached, the tag is likely "AttachedProp" which we don't want on a detached prop.
                detachingProp.tag = "Untagged";
                //Check to see if we have a rigidbody on the detaching object. We shouldn't, so let's add one.
                if (!detachingProp.GetComponent<Rigidbody>()) {

                    //re-adding rb to detaching prop.
                    detachingProp.AddComponent<Rigidbody>(); 
                    Rigidbody detPropRB = detachingProp.GetComponent<Rigidbody>();
                    //Re-enable networked movement/physics script on object.
                    PropRigidbodyTransformView prtv = detPropRB.GetComponent<PropRigidbodyTransformView>();
                    prtv.enabled = true;
                    //Get reference to prop's PV.
                    PhotonView detPropPV = detachingProp.GetComponent<PhotonView>();
                    //Set that networking script to be observed over the network.
                    detPropPV.ObservedComponents.Add(prtv);
                    //Make sure this detached prop has a RB, so we run a function under PropInteraction to ensure this.
                    detPropRB.gameObject.GetComponent<PropInteraction>().ResetRigidBodyAfterDetach();
                    //Set values of newly added RB. Add velocities after we unfreeze it.
                    detPropRB.isKinematic = false;
                    detPropRB.mass = massRef;
                    detPropRB.AddForce(velRef * detPropRB.mass, ForceMode.Impulse);
                    detPropRB.AddTorque(velAngRef * detPropRB.mass, ForceMode.Impulse);
                } else {
                    Debug.LogError("The prop we're trying to detach already has a rigidbody. This is an issue that needs to be fixed.");
                }
                //Make sure the newly detached prop is available over the network.
                detachingProp.GetComponent<PropInteraction>().isAvailable = true;
                //We need to count the children we detach. It should never detach more than one child. If we do, that's an issue.
                childrenDetached++;
                if (childrenDetached > 1) {
                    Debug.LogWarning("We detached all children from the player's PropHolder. But there was more than one?");
                }
            }

            //We set RotLocked to false so when we become a prop, our rotation is organically unlocked.
            if (changingPly.GetPhotonView().Owner.IsLocal) { //If we are the local player AND the targetPlayer.
                RotLocked = false;
            }

            // We need to store object data and send it through photonnetwork.instantiate because we need to reference out player.
            object[] InitData = new object[]
            {
                changingPly.GetPhotonView().ViewID
            };

            //Let's spawn a prop now that we detached the old prop.
            if (PhotonNetwork.IsMasterClient) { //If we are the MasterClient. We need to instantiate ALL props from MC, as they get deleted when the client leaves if they spawn them in.
                newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", ourOldPropName.ToString()), gameObject.transform.position, Quaternion.identity, 0, InitData);
                //We disable our Locked rotation so that we when we take over our new prop, we won't be locked.
            }

            //The rest gets handled from the PropInteraction script on the object when it spawn. This will parent it to us.
        }
    }

}


